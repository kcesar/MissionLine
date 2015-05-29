using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web;
using System.Web.Hosting;
using System.Xml.Linq;
using Kcesar.MissionLine.Website.Data;

namespace Kcesar.MissionLine.Website.Utils
{
    public class DatabaseIssuerNameRegistry : ValidatingIssuerNameRegistry
    {
        public static bool ContainsTenant(string tenantId)
        {
            using (MissionLineDbContext context = new MissionLineDbContext())
            {
                return context.Tenants
                    .Where(tenant => tenant.Id == tenantId)
                    .Any();
            }
        }

        public static bool ContainsKey(string thumbprint)
        {
            using (MissionLineDbContext context = new MissionLineDbContext())
            {
                return context.IssuingAuthorityKeys
                    .Where(key => key.Id == thumbprint)
                    .Any();
            }
        }

        public static void RefreshKeys(string metadataLocation)
        {
            IssuingAuthority issuingAuthority = ValidatingIssuerNameRegistry.GetIssuingAuthority(metadataLocation);

            bool newKeys = false;
            bool refreshTenant = false;
            foreach (string thumbprint in issuingAuthority.Thumbprints)
            {
                if (!ContainsKey(thumbprint))
                {
                    newKeys = true;
                    refreshTenant = true;
                    break;
                }
            }

            foreach (string issuer in issuingAuthority.Issuers)
            {
                if (!ContainsTenant(GetIssuerId(issuer)))
                {
                    refreshTenant = true;
                    break;
                }
            }

            if (newKeys || refreshTenant)
            {
                using (MissionLineDbContext context = new MissionLineDbContext())
                {
                    if (newKeys)
                    {
                      ((DbSet<IssuingAuthorityKey>)context.IssuingAuthorityKeys).RemoveRange(context.IssuingAuthorityKeys);
                      foreach (string thumbprint in issuingAuthority.Thumbprints)
                      {
                          context.IssuingAuthorityKeys.Add(new IssuingAuthorityKey { Id = thumbprint });
                      }
                    }

                    if (refreshTenant)
                    {
                        foreach (string issuer in issuingAuthority.Issuers)
                        {
                            string issuerId = GetIssuerId(issuer);
                            if (!ContainsTenant(issuerId))
                            {
                                context.Tenants.Add(new Tenant { Id = issuerId });
                            }
                        }
                    }
                    context.SaveChanges();
                }
            }
        }

        private static string GetIssuerId(string issuer)
        {
            return issuer.TrimEnd('/').Split('/').Last();
        }

        protected override bool IsThumbprintValid(string thumbprint, string issuer)
        {
            return ContainsTenant(GetIssuerId(issuer))
                && ContainsKey(thumbprint);
        }
    }
}
