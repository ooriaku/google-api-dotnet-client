﻿/*
Copyright 2015 Google Inc

Licensed under the Apache License, Version 2.0(the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using Google.Apis.Auth.OAuth2;
using Google.Apis.Tests.Mocks;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Google.Apis.Auth.Tests.OAuth2
{
    /// <summary>Tests for <see cref="Google.Apis.Auth.OAuth2.ComputeCredential"/>.</summary>
    public class ComputeCredentialTests
    {
        // Temporarily remove this test as we often are testing on GCE instances.
        /*[Test]
        public void IsRunningOnComputeEngine()
        {
            // It should be safe to assume that this test is not running on GCE.
            Assert.IsFalse(ComputeCredential.IsRunningOnComputeEngine().Result);
        }*/

        [Fact]
        public void IsRunningOnComputeEngine_ResultIsCached()
        {
            // Two subsequent invocations should return the same task.
            Assert.Same(ComputeCredential.IsRunningOnComputeEngine(),
                ComputeCredential.IsRunningOnComputeEngine());
        }

        [Fact]
        public async Task FetchesOidcToken()
        {
            var clock = new MockClock { UtcNow = new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) };
            var messageHandler = new OidcTokenSuccessMessageHandler(clock);
            var initializer = new ComputeCredential.Initializer("http://will.be.ignored", "http://will.be.ignored")
            {
                Clock = clock,
                HttpClientFactory = new MockHttpClientFactory(messageHandler)
            };
            var credential = new ComputeCredential(initializer);

            var oidcToken = await credential.GetOidcTokenAsync(OidcTokenOptions.FromTargetAudience("audience"));

            Assert.Equal("very_fake_access_token_1", await oidcToken.GetAccessTokenAsync());
            // Move the clock some but not enough that the token expires.
            clock.UtcNow = clock.UtcNow.AddMinutes(20);
            Assert.Equal("very_fake_access_token_1", await oidcToken.GetAccessTokenAsync());
            // Only the first call should have resulted in a request. The second time the token hadn't expired.
            Assert.Equal(1, messageHandler.Calls);
        }

        [Fact]
        public async Task RefreshesOidcToken()
        {
            var clock = new MockClock { UtcNow = new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) };
            var messageHandler = new OidcTokenSuccessMessageHandler(clock);
            var initializer = new ComputeCredential.Initializer("http://will.be.ignored", "http://will.be.ignored")
            {
                Clock = clock,
                HttpClientFactory = new MockHttpClientFactory(messageHandler)
            };
            var credential = new ComputeCredential(initializer);

            var oidcToken = await credential.GetOidcTokenAsync(OidcTokenOptions.FromTargetAudience("audience"));

            Assert.Equal("very_fake_access_token_1", await oidcToken.GetAccessTokenAsync());
            // Move the clock so that the token expires.
            clock.UtcNow = clock.UtcNow.AddHours(2);
            Assert.Equal("very_fake_access_token_2", await oidcToken.GetAccessTokenAsync());
            // Two calls, because the second time we tried to get the token, the first one had expired.
            Assert.Equal(2, messageHandler.Calls);
        }

        [Fact]
        public async Task FetchesOidcToken_WithDefaultOptions()
        {
            var clock = new MockClock { UtcNow = new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) };
            var messageHandler = new OidcTokenSuccessMessageHandler(clock);
            var initializer = new ComputeCredential.Initializer("http://will.be.ignored", "http://will.be.ignored")
            {
                Clock = clock,
                HttpClientFactory = new MockHttpClientFactory(messageHandler)
            };
            var credential = new ComputeCredential(initializer);

            var oidcToken = await credential.GetOidcTokenAsync(OidcTokenOptions.FromTargetAudience("any_audience"));
            await oidcToken.GetAccessTokenAsync();

            Assert.Equal("?audience=any_audience&format=full", messageHandler.LatestRequest.RequestUri.Query);
        }

        [Theory]
        [InlineData(OidcTokenFormat.Full, "another_audience", "?audience=another_audience&format=full")]
        [InlineData(OidcTokenFormat.Standard, "another_audience", "?audience=another_audience")]
        [InlineData(OidcTokenFormat.FullWithLicences, "another_audience", "?audience=another_audience&format=full&licenses=true")]
        public async Task FetchesOidcToken_WithOptions(OidcTokenFormat format, string targetAudience, string expectedQueryString)
        {
            var clock = new MockClock { UtcNow = new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) };
            var messageHandler = new OidcTokenSuccessMessageHandler(clock);
            var initializer = new ComputeCredential.Initializer("http://will.be.ignored", "http://will.be.ignored")
            {
                Clock = clock,
                HttpClientFactory = new MockHttpClientFactory(messageHandler)
            };
            var credential = new ComputeCredential(initializer);

            var oidcToken = await credential.GetOidcTokenAsync(
                OidcTokenOptions.FromTargetAudience("any_audience")
                .WithTargetAudience(targetAudience)
                .WithTokenFormat(format));
            await oidcToken.GetAccessTokenAsync();

            Assert.Equal(expectedQueryString, messageHandler.LatestRequest.RequestUri.Query);
        }
    }
}
