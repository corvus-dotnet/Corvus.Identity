// <copyright file="ConfigLoading.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity
{
    using System.IO;
    using System.Text;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Utilities for loading data as IConfiguration.
    /// </summary>
    public static class ConfigLoading
    {
        public static T LoadJsonConfiguration<T>(string configurationJson)
        {
            var configurationJsonStream = new MemoryStream(Encoding.UTF8.GetBytes(configurationJson));

            IConfigurationRoot configRoot = new ConfigurationBuilder()
                .AddJsonStream(configurationJsonStream)
                .Build();

            return configRoot.Get<T>();
        }
    }
}