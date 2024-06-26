﻿using AutoMapper;
using EasyOC.Core.Application;
using EasyOC.Core.DtoModels;
using EasyOC.DynamicWebApi.Attributes;
using EasyOC.OpenApi.Dto;
using EasyOC.OpenApi.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using OrchardCore.Data.Documents;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Environment.Extensions;
using OrchardCore.Roles;
using OrchardCore.Security;
using OrchardCore.Security.Permissions;
using OrchardCore.Security.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EasyOC.OpenApi.Services
{
    public class RolesAppService : AppServiceBase, IRolesAppService
    {
        private readonly RoleManager<IRole> _roleManager;
        private readonly IRoleService _roleService;
        private readonly IEnumerable<IPermissionProvider> _permissionProviders;
        private readonly IAuthorizationService _authorizationService;
        private readonly ITypeFeatureProvider _typeFeatureProvider;
        private readonly INotifier _notifier;
        private readonly IDocumentStore _documentStore;



        public RolesAppService(IRoleService roleService, RoleManager<IRole> roleManager
            , IEnumerable<IPermissionProvider> permissionProviders
            , ITypeFeatureProvider typeFeatureProvider, IDocumentStore documentStore
            , IAuthorizationService authorizationService, INotifier notifier)
        {
            _roleService = roleService;
            _roleManager = roleManager;
            _permissionProviders = permissionProviders;
            _typeFeatureProvider = typeFeatureProvider;
            _documentStore = documentStore;
            _authorizationService = authorizationService;
            _notifier = notifier;
        }

        public async Task<List<RoleDto>> GetRolesAsync()
        {
            var roles = await _roleService.GetRolesAsync();

            return roles.Select(ObjectMapper.Map<RoleDto>).ToList();
        }
        [EOCAuthorization("ManageRoles")]
        public async Task<RoleDetailsDto> GetRoleDetailsAsync(string id)
        {

            var role = (Role)await _roleManager.FindByNameAsync(_roleManager.NormalizeKey(id));
            if (role == null)
            {
                //return NotFound();
                throw new ArgumentException($"Role: {role} Not Found");
            }

            var installedPermissions = await GetInstalledPermissionsAsync();
            var allPermissions = installedPermissions.SelectMany(x => x.Value);

            var model = new RoleDetailsDto
            {
                Name = role.RoleName,
                RoleDescription = role.RoleDescription,
                Permissions = await GetEffectivePermissions(role, allPermissions),
                //RoleCategoryPermissions = ObjectMapper.Map<IDictionary<string, IEnumerable<PermissionDto>>>(installedPermissions),
                VbenMenuIds = role.RoleClaims.Where(r => r.ClaimType == RoleClaimType.VbenMenuId.ToString()).Select(x => x.ClaimValue),
            };

            return model;
        }
        [EOCAuthorization("ManageRoles")]
        public async Task CreateRoleAsync(RoleDto model)
        {

            model.RoleName = model.RoleName.Trim();

            if (model.RoleName.Contains('/'))
            {
                await _notifier.ErrorAsync(H["Invalid role name."]);
            }

            if (await _roleManager.FindByNameAsync(_roleManager.NormalizeKey(model.RoleName)) != null)
            {
                await _notifier.ErrorAsync(H["The role is already used."]);
            }


            var role = new Role { RoleName = model.RoleName, RoleDescription = model.RoleDescription };
            var result = await _roleManager.CreateAsync(role);
            if (result.Succeeded)
            {
                await _notifier.SuccessAsync(H["Role created successfully."]);
                return;
            }

            await _documentStore.CancelAsync();

            foreach (var error in result.Errors)
            {
                await _notifier.ErrorAsync(H[error.Description]);
            }
        }
        [EOCAuthorization("ManageRoles")]
        public async Task DeleteRoleAsync(string id)
        {
            //if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageRoles))
            //{
            //     throw new ArgumentException($"Role: {currentRole} Not Found");
            //}

            var currentRole = await _roleManager.FindByIdAsync(id);

            if (currentRole == null)
            {
                throw new ArgumentException($"Role: {currentRole} Not Found");
            }

            var result = await _roleManager.DeleteAsync(currentRole);

            if (result.Succeeded)
            {
                await _notifier.SuccessAsync(H["Role deleted successfully."]);
            }
            else
            {
                await _documentStore.CancelAsync();

                await _notifier.ErrorAsync(H["Could not delete this role."]);

                foreach (var error in result.Errors)
                {
                    await _notifier.ErrorAsync(H[error.Description]);
                }
            }
        }
        [EOCAuthorization("ManageRoles")]
        public async Task UpdateRoleAsync(RoleDetailsDto input)
        {
            var role = (Role)await _roleManager.FindByNameAsync(_roleManager.NormalizeKey(input.Name));

            if (role == null)
            {
                throw new ArgumentException($"Role: {role} Not Found");
            }

            role.RoleDescription = input.RoleDescription;

            role.RoleClaims.Clear();
            if (input.Permissions is not null && input.Permissions.Any())
            {
                role.RoleClaims.AddRange(input.Permissions
                   .Select(x => new RoleClaim
                   {
                       ClaimType = RoleClaimType.Permission.ToString(),
                       ClaimValue = x
                   }));
            }
            if (input.VbenMenuIds is not null && input.VbenMenuIds.Any())
            {
                role.RoleClaims.AddRange(input.VbenMenuIds
                  .Select(x => new RoleClaim
                  {
                      ClaimType = RoleClaimType.VbenMenuId.ToString(),
                      ClaimValue = x
                  }));
            }
            await _roleManager.UpdateAsync(role);

            await _notifier.SuccessAsync(H["Role updated successfully."]);

        }

        [IgnoreWebApiMethod]
        public async Task<IDictionary<string, IEnumerable<Permission>>> GetInstalledPermissionsAsync()
        {
            var installedPermissions = new Dictionary<string, IEnumerable<Permission>>();
            foreach (var permissionProvider in _permissionProviders)
            {
                var feature = _typeFeatureProvider.GetFeatureForDependency(permissionProvider.GetType());
                var featureName = feature.Id;

                var permissions = await permissionProvider.GetPermissionsAsync();

                foreach (var permission in permissions)
                {
                    var category = permission.Category;

                    string title = String.IsNullOrWhiteSpace(category) ? S["{0} Feature", featureName] : category;

                    if (installedPermissions.ContainsKey(title))
                    {
                        installedPermissions[title] = installedPermissions[title].Concat(new[] { permission });
                    }
                    else
                    {
                        installedPermissions.Add(title, new[] { permission });
                    }
                }
            }

            return installedPermissions;
        }
        public async Task<IDictionary<string, IEnumerable<PermissionDto>>> GetAllPermissionsAsync()
        {
            var installedPermissions = await GetInstalledPermissionsAsync();

            return ObjectMapper.Map<IDictionary<string, IEnumerable<PermissionDto>>>(installedPermissions);
        }
        [IgnoreWebApiMethod]
        public async Task<IEnumerable<string>> GetEffectivePermissions(Role role, IEnumerable<Permission> allPermissions)
        {
            // Create a fake user to check the actual permissions. If the role is anonymous
            // IsAuthenticated needs to be false.
            var fakeIdentity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role.RoleName) },
                role.RoleName != "Anonymous" ? "FakeAuthenticationType" : null);

            // Add role claims
            fakeIdentity.AddClaims(role.RoleClaims.Where(c => c.ClaimType == RoleClaimType.Permission.ToString()).Select(c => c.ToClaim()));

            var fakePrincipal = new ClaimsPrincipal(fakeIdentity);

            var result = new List<string>();

            foreach (var permission in allPermissions)
            {
                if (await _authorizationService.AuthorizeAsync(fakePrincipal, permission))
                {
                    result.Add(permission.Name);
                }
            }

            return result;
        }

    }
}
