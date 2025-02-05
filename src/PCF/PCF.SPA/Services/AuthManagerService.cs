﻿using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

namespace PCF.SPA.Services
{
    public class AuthenticationManager : AuthenticationStateProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorageService;
        private readonly AuthenticationState _anonymous;

        public event Action? OnAuthenticationStateChanged;

        public AuthenticationManager(HttpClient httpClient, ILocalStorageService localStorageService)
        {
            _httpClient = httpClient;
            _localStorageService = localStorageService;
            _anonymous = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _localStorageService.GetItemAsync<string>("authToken");

            if (string.IsNullOrWhiteSpace(token) || IsTokenExpired(token))
            {
                return _anonymous;
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var claims = JwtParser.ParseClaimsFromJwt(token);
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "jwtAuthType")));
        }
        public async Task<bool> LoginAsync(LoginResponseDto loginResponseDto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("auth/login", loginResponseDto);
                if (response.IsSuccessStatusCode)
                {
                    var token = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        await SaveTokenAsync(token);
                        NotifyAuthenticationStateChanged();
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no login: {ex.Message}");
                return false;
            }
        }
        public async Task LogoutAsync()
        {
            try
            {
                await RemoveTokenAsync();
                NotifyUserLogout();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao realizar logout: {ex.Message}");
                throw;
            }
        }
        public async Task<string> RegisterAsync(LoginResponseDto loginResponseDto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("auth/register", loginResponseDto);
                if (response.IsSuccessStatusCode)
                {
                    return "Registrado com sucesso";
                }

                var errorMessage = await response.Content.ReadAsStringAsync();
                return string.IsNullOrWhiteSpace(errorMessage) ? "Falha ao registrar usuário." : errorMessage;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao registrar usuário: {ex.Message}");
                return "Erro ao registrar usuário. Tente novamente mais tarde.";
            }
        }
        public async Task<bool> IsLoggedInAsync()
        {
            var authState = await GetAuthenticationStateAsync();
            return authState.User.Identity?.IsAuthenticated ?? false;
        }
        private async Task SaveTokenAsync(string token)
        {
            await _localStorageService.SetItemAsync("authToken", token);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        private async Task RemoveTokenAsync()
        {
            await _localStorageService.RemoveItemAsync("authToken");
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
        private bool IsTokenExpired(string token)
        {
            var claims = JwtParser.ParseClaimsFromJwt(token);
            var expiry = claims.FirstOrDefault(c => c.Type == "exp");
            if (expiry == null) return true;

            var expiryDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expiry.Value)).UtcDateTime;
            return expiryDate <= DateTime.UtcNow;
        }
        private void NotifyUserLogout()
        {
            NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
            OnAuthenticationStateChanged?.Invoke();
        }
        private void NotifyAuthenticationStateChanged()
        {
            var authState = GetAuthenticationStateAsync();
            NotifyAuthenticationStateChanged(authState);
            OnAuthenticationStateChanged?.Invoke();
        }
    }
}
