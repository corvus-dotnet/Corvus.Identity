// <copyright file="CertificateNotFoundException.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.Certificates
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Indicates that the specified certificate was not found.
    /// </summary>
    public class CertificateNotFoundException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="CertificateNotFoundException"/>.
        /// </summary>
        public CertificateNotFoundException()
            : base("Certificate was not found.")
        {
        }
    }
}