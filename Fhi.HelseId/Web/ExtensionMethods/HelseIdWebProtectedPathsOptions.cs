using System.Collections.Generic;
using System.Linq;
using Fhi.HelseId.Web.Middleware;
using Microsoft.AspNetCore.Http;

namespace Fhi.HelseId.Web.ExtensionMethods
{
    public class HelseIdWebProtectedPathsOptions : ProtectPathsOptions
    {
        public HelseIdWebProtectedPathsOptions(string policy, IRedirectPagesKonfigurasjon redirect,
            IReadOnlyCollection<PathString> excludeList) : base(policy,redirect.Forbidden)
        {
            var excluded = new List<PathString>
            {
                "/favicon.ico",
                redirect.Forbidden,
                redirect.LoggedOut,
                redirect.Statuscode
            };
            if (excludeList != null && excludeList.Any())
                excluded.AddRange(excludeList);
            Exclusions = excluded;
        }
    }
}