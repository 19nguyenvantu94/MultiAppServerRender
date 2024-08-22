﻿using BlazorIdentity.Data;
using BlazorIdentity.Users.Models;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;


namespace BlazorIdentity.Users.Services
{
    public class ProfileService : IProfileService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ApplicationDbContext _applicationDbContext;

        public ProfileService(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, ApplicationDbContext applicationDbContext)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _applicationDbContext = applicationDbContext;
        }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var subject = context.Subject ?? throw new ArgumentNullException(nameof(context.Subject));

            var subjectId = subject.Claims.Where(x => x.Type == "sub").FirstOrDefault()?.Value;

            var user = await _userManager.FindByIdAsync(subjectId);
            if (user == null)
                throw new ArgumentException("Invalid subject identifier");

            var claims = await GetClaimsFromUser(user);
            context.IssuedClaims = claims.ToList();
        }

        public async Task IsActiveAsync(IsActiveContext context)
        {
            var subject = context.Subject ?? throw new ArgumentNullException(nameof(context.Subject));

            var subjectId = subject.Claims.Where(x => x.Type == "sub").FirstOrDefault()?.Value;
            var user = await _userManager.FindByIdAsync(subjectId);

            context.IsActive = false;

            if (user != null)
            {
                if (_userManager.SupportsUserSecurityStamp)
                {
                    var security_stamp = subject.Claims.Where(c => c.Type == "security_stamp").Select(c => c.Value).SingleOrDefault();
                    if (security_stamp != null)
                    {
                        var db_security_stamp = await _userManager.GetSecurityStampAsync(user);
                        if (db_security_stamp != security_stamp)
                            return;
                    }
                }

                context.IsActive =
                    !user.LockoutEnabled ||
                    !user.LockoutEnd.HasValue ||
                    user.LockoutEnd <= DateTime.UtcNow;
            }
        }

        private async Task<List<Claim>> GetClaimsFromUser(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtClaimTypes.Subject, user.Id.ToString()),
                new Claim(JwtClaimTypes.PreferredUserName, user.UserName),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName)
            };

            //if (!string.IsNullOrWhiteSpace(user.Name))
            //    claims.Add(new Claim("name", user.Name));

            //if (!string.IsNullOrWhiteSpace(user.LastName))
            //    claims.Add(new Claim("last_name", user.LastName));

            //if (!string.IsNullOrWhiteSpace(user.CardNumber))
            //    claims.Add(new Claim("card_number", user.CardNumber));

            //if (!string.IsNullOrWhiteSpace(user.CardHolderName))
            //    claims.Add(new Claim("card_holder", user.CardHolderName));

            //if (!string.IsNullOrWhiteSpace(user.SecurityNumber))
            //    claims.Add(new Claim("card_security_number", user.SecurityNumber));

            //if (!string.IsNullOrWhiteSpace(user.Expiration))
            //    claims.Add(new Claim("card_expiration", user.Expiration));

            //if (!string.IsNullOrWhiteSpace(user.City))
            //    claims.Add(new Claim("address_city", user.City));

            //if (!string.IsNullOrWhiteSpace(user.Country))
            //    claims.Add(new Claim("address_country", user.Country));

            //if (!string.IsNullOrWhiteSpace(user.State))
            //    claims.Add(new Claim("address_state", user.State));

            //if (!string.IsNullOrWhiteSpace(user.Street))
            //    claims.Add(new Claim("address_street", user.Street));

            //if (!string.IsNullOrWhiteSpace(user.ZipCode))
            //    claims.Add(new Claim("address_zip_code", user.ZipCode));

            if (_userManager.SupportsUserEmail)
            {
                claims.AddRange(new[]
                {
                    new Claim(JwtClaimTypes.Email, user.Email),
                    new Claim(JwtClaimTypes.EmailVerified, user.EmailConfirmed ? "true" : "false", ClaimValueTypes.Boolean)
                });
            }

            if (_userManager.SupportsUserPhoneNumber && !string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                claims.AddRange(new[]
                {
                    new Claim(JwtClaimTypes.PhoneNumber, user.PhoneNumber),
                    new Claim(JwtClaimTypes.PhoneNumberVerified, user.PhoneNumberConfirmed ? "true" : "false", ClaimValueTypes.Boolean)
                });
            }

            var lstRole = await _applicationDbContext.UserRoles.Where(x => x.UserId == user.Id).Include(x => x.Role).Select(x => x.Role).ToListAsync();

            foreach (var item in lstRole)
            {
                claims.Add(new Claim(JwtClaimTypes.Role, item.Name));

                foreach (var item2 in await _roleManager.GetClaimsAsync(item))
                {
                    claims.Add(new Claim(JwtClaimTypes.Role, item2.Value));
                }
            }

            return claims;
        }
    }
}
