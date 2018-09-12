// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace IdentityService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.Title = "IdentityService";

            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseUrls("http://0.0.0.0:5001/")
                .UseKestrel(options => options.AddServerHeader = false)
                .UseStartup<Startup>()
                .Build();
    }
}