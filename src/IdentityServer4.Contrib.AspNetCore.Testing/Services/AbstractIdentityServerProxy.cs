using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel;
using IdentityModel.Client;
using IdentityServer4.Contrib.AspNetCore.Testing.Configuration;
using IdentityServer4.Models;
using Microsoft.AspNetCore.TestHost;

namespace IdentityServer4.Contrib.AspNetCore.Testing.Services
{
    public abstract class AbstractIdentityServerProxy
    {
        public abstract TestServer IdentityServer { get; }

        public async Task<DiscoveryDocumentResponse> GetDiscoverResponseAsync()
        {
            using var proxy = this.IdentityServer.CreateHandler();
            using var client = new HttpClient(proxy);
            return await client.GetDiscoveryDocumentAsync(this.IdentityServer.BaseAddress.ToString());
        }

        public async Task<TokenResponse> GetTokenAsync(ClientConfiguration clientConfiguration, string grantType,
            Parameters parameters)
        {
            var discoveryResponse = await this.GetDiscoverResponseAsync();

            EnsureDiscoverResponse(discoveryResponse);

            using var proxy = this.IdentityServer.CreateHandler();
            using var client = new HttpClient(proxy);

            return await client.RequestTokenAsync(new TokenRequest
            {
                Address = discoveryResponse.TokenEndpoint,
                ClientId = clientConfiguration.Id,
                ClientSecret = clientConfiguration.Secret,
                GrantType = grantType,
                Parameters = parameters
            });
        }

        public async Task<TokenResponse> GetClientAccessTokenAsync(ClientConfiguration clientConfiguration,
            params string[] scopes)
        {
            var discoveryResponse = await this.GetDiscoverResponseAsync();

            EnsureDiscoverResponse(discoveryResponse);

            using var proxy = this.IdentityServer.CreateHandler();
            using var client = new HttpClient(proxy);

            return await ExecuteClientAccessTokenRequestAsync(client, discoveryResponse, clientConfiguration,
                new Parameters(), scopes);
        }

        public async Task<TokenResponse> GetClientAccessTokenAsync(ClientConfiguration clientConfiguration,
            Parameters parameters, params string[] scopes)
        {
            var discoveryResponse = await this.GetDiscoverResponseAsync();

            EnsureDiscoverResponse(discoveryResponse);

            var usedParameters = parameters ?? new Parameters();

            using var proxy = this.IdentityServer.CreateHandler();
            using var client = new HttpClient(proxy);

            return await ExecuteClientAccessTokenRequestAsync(client, discoveryResponse, clientConfiguration,
                usedParameters, scopes);
        }

        public async Task<TokenResponse> GetResourceOwnerPasswordAccessTokenAsync(
            ClientConfiguration clientConfiguration, UserLoginConfiguration userLoginConfiguration,
            params string[] scopes)
        {
            var discoveryResponse = await this.GetDiscoverResponseAsync();

            EnsureDiscoverResponse(discoveryResponse);

            using var proxy = this.IdentityServer.CreateHandler();
            using var client = new HttpClient(proxy);

            return await ExecuteResourceOwnerAccessTokenRequestAsync(client, discoveryResponse,
                clientConfiguration, userLoginConfiguration, new Parameters(), scopes);
        }

        public async Task<TokenResponse> GetResourceOwnerPasswordAccessTokenAsync(
            ClientConfiguration clientConfiguration, UserLoginConfiguration userLoginConfiguration,
            Parameters parameters)
        {
            var discoveryResponse = await this.GetDiscoverResponseAsync();

            EnsureDiscoverResponse(discoveryResponse);

            using var proxy = this.IdentityServer.CreateHandler();
            using var client = new HttpClient(proxy);

            return await ExecuteResourceOwnerAccessTokenRequestAsync(client, discoveryResponse,
                clientConfiguration, userLoginConfiguration, parameters, Array.Empty<string>());
        }

        public async Task<TokenResponse> GetRefreshTokenAsync(ClientConfiguration clientConfiguration,
            string refreshToken, params string[] scopes)
        {
            var discoveryResponse = await this.GetDiscoverResponseAsync();

            EnsureDiscoverResponse(discoveryResponse);

            using var proxy = this.IdentityServer.CreateHandler();
            using var client = new HttpClient(proxy);

            return await ExecuteRefreshAccessTokenRequestAsync(client, discoveryResponse, clientConfiguration,
                refreshToken, new Parameters(), scopes);
        }

        public async Task<TokenResponse> GetRefreshTokenAsync(ClientConfiguration clientConfiguration,
            string refreshToken, Parameters parameters)
        {
            var discoveryResponse = await this.GetDiscoverResponseAsync();

            EnsureDiscoverResponse(discoveryResponse);

            using var proxy = this.IdentityServer.CreateHandler();
            using var client = new HttpClient(proxy);

            return await ExecuteRefreshAccessTokenRequestAsync(client, discoveryResponse, clientConfiguration,
                refreshToken, parameters, Array.Empty<string>());
        }

        public async Task<UserInfoResponse> GetUserInfoAsync(string accessToken)
        {
            var discoveryResponse = await this.GetDiscoverResponseAsync();

            EnsureDiscoverResponse(discoveryResponse);

            using var proxy = this.IdentityServer.CreateHandler();
            using var client = new HttpClient(proxy);

            return await client.GetUserInfoAsync(new UserInfoRequest
            {
                Address = discoveryResponse.UserInfoEndpoint,
                Token = accessToken
            });
        }

        private static Task<TokenResponse> ExecuteResourceOwnerAccessTokenRequestAsync(HttpMessageInvoker client,
            DiscoveryDocumentResponse discoveryResponse,
            ClientConfiguration clientConfiguration, UserLoginConfiguration userLoginConfiguration,
            Parameters parameters, IEnumerable<string> scopes)
            => client.RequestPasswordTokenAsync(new PasswordTokenRequest
            {
                UserName = userLoginConfiguration.Username,
                Password = userLoginConfiguration.Password,
                ClientId = clientConfiguration.Id,
                ClientSecret = clientConfiguration.Secret,
                Address = discoveryResponse.TokenEndpoint,
                GrantType = GrantType.ResourceOwnerPassword,
                Scope = string.Join(" ", scopes),
                Parameters = parameters
            });

        private static Task<TokenResponse> ExecuteRefreshAccessTokenRequestAsync(HttpMessageInvoker client,
            DiscoveryDocumentResponse discoveryResponse,
            ClientConfiguration clientConfiguration, string refreshToken, Parameters parameters,
            IEnumerable<string> scopes)
            => client.RequestRefreshTokenAsync(new RefreshTokenRequest
            {
                RefreshToken = refreshToken,
                ClientId = clientConfiguration.Id,
                ClientSecret = clientConfiguration.Secret,
                Address = discoveryResponse.TokenEndpoint,
                GrantType = GrantType.ResourceOwnerPassword,
                Scope = string.Join(" ", scopes),
                Parameters = parameters
            });

        private static Task<TokenResponse> ExecuteClientAccessTokenRequestAsync(HttpMessageInvoker client,
            DiscoveryDocumentResponse discoveryResponse,
            ClientConfiguration clientConfiguration, Parameters parameters, IEnumerable<string> scopes)
            => client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = discoveryResponse.TokenEndpoint,
                ClientId = clientConfiguration.Id,
                ClientSecret = clientConfiguration.Secret,
                GrantType = OidcConstants.GrantTypes.ClientCredentials,
                Scope = string.Join(" ", scopes),
                Parameters = parameters
            });

        private static void EnsureDiscoverResponse(ProtocolResponse discoveryResponse)
        {
            if (!discoveryResponse.IsError) return;

            throw new InvalidOperationException(
                $"Cannot continue since discover-request has failed with message:\n{discoveryResponse.Error}");
        }
    }
}