﻿using System;
using System.Collections.Generic;
using System.ServiceModel;
using Fhi.HelseId.Web.Hpr.Core;
using HprServiceReference;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace Fhi.HelseId.Web.Hpr
{
    public interface IHprFactory
    {
        IHPR2ServiceChannel? ServiceProxy { get; }
        IHprService CreateHprRepository();
    }

    public interface IGodkjenteHprKategoriListe
    {
        IEnumerable<OId9060> Godkjenninger { get; }
    }

    /// <summary>
    /// Lag subklasse og sett opp for injection
    /// Legg til i denne listen de ekstra kategoriene utover Lege som er default godkjent
    /// </summary>
    public abstract class GodkjenteHprKategoriListe : IGodkjenteHprKategoriListe
    {
        private readonly List<OId9060> godkjenninger = new List<OId9060>();

        protected void Add(OId9060 godkjent) => godkjenninger.Add(godkjent);

        public IEnumerable<OId9060> Godkjenninger => godkjenninger;
    }

    public class HprFactory : IHprFactory
    {
        private readonly IGodkjenteHprKategoriListe godkjenninger;
        private readonly ILogger<HprFactory> logger;
        public IHPR2ServiceChannel? ServiceProxy { get; }

        public HprFactory(IOptions<HprKonfigurasjon> hprKonfigurasjon, IGodkjenteHprKategoriListe godkjenninger, ILogger<HprFactory> logger)
        {
            this.godkjenninger = godkjenninger;
            this.logger = logger;
            var config = hprKonfigurasjon.Value;
            this.logger.LogDebug("Access til HPR: {Url}", config.Url);
            if (!config.UseHpr)
            {
                this.logger.LogInformation("HprFactory: Hpr er avslått, se konfigurasjon");
                return;
            }

            var userName = config.Brukernavn;
            var passord = config.Passord;
            var httpBinding = new WSHttpBinding(SecurityMode.Transport);
            httpBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;

            var channelFactory = new ChannelFactory<IHPR2ServiceChannel>(httpBinding, new EndpointAddress(new Uri(config.Url)));
            channelFactory.Credentials.UserName.UserName = userName;
            channelFactory.Credentials.UserName.Password = passord;
            ServiceProxy = channelFactory.CreateChannel();
            ServiceProxy.Open();
        }

       

        public IHprService CreateHprRepository() => new HprService(this,logger).LeggTilGodkjenteHelsepersonellkategorier(godkjenninger);
    }
}