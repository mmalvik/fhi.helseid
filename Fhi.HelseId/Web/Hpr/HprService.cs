﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fhi.HelseId.Web.Hpr.Core;
using HprServiceReference;
using Microsoft.Extensions.Logging;

namespace Fhi.HelseId.Web.Hpr
{
    public interface IHprService
    {
        Task<bool> SjekkGodkjenning(string hprnummer);
        Task<Person?> HentPerson(string hprnummer);

        /// <summary>
        /// Sjekker om personen har gyldig aktiv autorisasjon som en av de godkjente kategoriene.  Default kategori er Lege
        /// </summary>
        bool ErGyldig(Person person);

        void Close();
        IHprService LeggTilGodkjenteHelsepersonellkategori(OId9060 ny);
        IHprService LeggTilGodkjenteHelsepersonellkategorier(IGodkjenteHprKategoriListe liste);
        bool ErGyldigForKategorier(Person person, params OId9060[] koder);
    }

    public class HprService : IHprService
    {
        private readonly IHPR2ServiceChannel? serviceClient;
        private readonly ILogger logger;

        private List<OId9060> GodkjenteHelsepersonellkategorier { get; }


        const string HprnummerAdmin = "000000000";


        public HprService(IHprFactory helsepersonellFactory, ILogger logger)
        {
            this.logger = logger;
            serviceClient = helsepersonellFactory.ServiceProxy;
            GodkjenteHelsepersonellkategorier = new List<OId9060>();
        }

        public IHprService LeggTilGodkjenteHelsepersonellkategorier(IGodkjenteHprKategoriListe liste)
        {
            foreach (var godkjent in liste.Godkjenninger)
                LeggTilGodkjenteHelsepersonellkategori(godkjent);
            return this;
        }

        public IHprService LeggTilGodkjenteHelsepersonellkategori(OId9060 ny)
        {
            GodkjenteHelsepersonellkategorier.Add(ny);
            return this;
        }

        public async Task<bool> SjekkGodkjenning(string hprnummer)
        {
            if (hprnummer == HprnummerAdmin)
                return true;
            var person = await HentFraHprRegister(hprnummer);
            return person != null && ErGyldig(person);
        }

        public async Task<Person?> HentPerson(string hprnummer)
        {
            var person = await HentFraHprRegister(hprnummer);
            return person;
        }


        private async Task<Person?> HentFraHprRegister(string hprnummer)
        {
            if (serviceClient == null)
            {
                logger.LogError("Kunne ikke skape connection til Hpr register");
                return null;
            }

            try
            {
                var person = await serviceClient.HentPersonAsync(Convert.ToInt32(hprnummer), null);
                return person;
            }
            catch (System.ServiceModel.CommunicationException e)
            {
                logger.LogError(e, "CommunicationException i aksess til Hpr register. ");
                return null;
            }
#pragma warning disable 168
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
#pragma warning restore 168
            {
                //Hvis ekstern service kaster exception returneres null. Eksemplvis mottar vi også en exception hvis fnr ikke finnes.
                logger.LogError(e, "Feil i aksess til Hpr register. (Obs: Mottar også en exception hvis fnr ikke finnes)");
                return null;
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        /// <summary>
        /// Sjekker om personen har gyldig aktiv autorisasjon som en av de godkjente kategoriene.  Default kategori er Lege
        /// </summary>
        public bool ErGyldig(Person person) => ErGyldigForKategorier(person, GodkjenteHelsepersonellkategorier.ToArray());
        
        public bool ErGyldigForKategorier(Person person, params OId9060[] koder)
        {
            if (person == null)
                return false;

            return person.Godkjenninger.Any(ErAktivGodkjenning);

            bool ErAktivGodkjenning(Godkjenning g)
            {
                return koder.Select(x=>x.ToString()).Contains(g.Helsepersonellkategori.Verdi)
                       && g.Gyldig.Aktiv()
                       && !g.Suspensjonsperioder.Any(s => s.Periode.Aktiv());
            }
        }

        public async void Close()
        {
            if (serviceClient != null)
            {
                if (serviceClient is HPR2ServiceClient client) await client.CloseAsync();
            }
        }

    }

    public static class HprExtensionMethods
    {
        public static bool Aktiv(this Periode periode) => periode.Fra < DateTime.Now && (periode.Til == null || periode.Til > DateTime.Now);
    }

}