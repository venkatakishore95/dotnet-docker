// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.DotNet.Docker.Tests
{
    public class ImageData
    {
        private List<string> _pulledImages = new List<string>();
        private Version _runtimeDepsVersion;
        private string _sdkOS;

        public Arch Arch { get; set; }
        public Version Version { get; set; }
        public string VersionString => Version.ToString(2);
        public bool HasSdk => _sdkOS != null;
        public bool IsArm => Arch == Arch.Arm || Arch == Arch.Arm64;
        public string OS { get; set; }

        public string Rid
        {
            get {
                string rid;

                if (Arch == Arch.Arm)
                {
                    rid = "linux-arm";
                }
                else if (Arch == Arch.Arm64)
                {
                    if (OS.StartsWith(Tests.OS.AlpinePrefix))
                    {
                        rid = "linux-musl-arm64";
                    }
                    else
                    {
                        rid = "linux-arm64";
                    }
                }
                else if (OS.StartsWith(Tests.OS.AlpinePrefix))
                {
                    rid = "linux-musl-x64";
                }
                else
                {
                    rid = "linux-x64";
                }

                return rid;
            }
        }

        public Version RuntimeDepsVersion
        {
            get { return _runtimeDepsVersion ?? Version; }
            set { _runtimeDepsVersion = value; }
        }

        public string SdkOS
        {
            get => _sdkOS ?? OS.TrimEnd(Tests.OS.SlimSuffix);
            set { _sdkOS = value; }
        }

        public string GetIdentifier(string type) => $"{VersionString}-{type}-{DateTime.Now.ToFileTime()}";

        public string GetImage(DotNetImageType imageType, DockerHelper dockerHelper)
        {
            string imageName = GetImageName(imageType);

            if (!Config.IsLocalRun && !_pulledImages.Contains(imageName))
            {
                dockerHelper.Pull(imageName);
                _pulledImages.Add(imageName);
            }
            else
            {
                Assert.True(DockerHelper.ImageExists(imageName), $"`{imageName}` could not be found on disk.");
            }

            return imageName;
        }

        private string GetImageName(DotNetImageType imageType)
        {
            string repoSuffix = Config.IsNightlyRepo ? "-nightly" : string.Empty;
            string variantName = Enum.GetName(typeof(DotNetImageType), imageType).ToLowerInvariant().Replace('_', '-');

            Version imageVersion;
            string os;
            switch (imageType)
            {
                case DotNetImageType.Runtime:
                case DotNetImageType.Aspnet:
                    imageVersion = Version;
                    os = OS;
                    break;
                case DotNetImageType.Runtime_Deps:
                    imageVersion = RuntimeDepsVersion;
                    os = OS;
                    break;
                case DotNetImageType.SDK:
                    imageVersion = Version;
                    os = SdkOS;
                    break;
                default:
                    throw new NotSupportedException($"Unsupported image type '{variantName}'");
            }

            string arch = string.Empty;
            if (Arch == Arch.Arm)
            {
                arch = "-arm32v7";
            }
            else if (Arch == Arch.Arm64)
            {
                arch = "-arm64v8";
            }

            return $"{Config.Registry}/{Config.RepoPrefix}dotnet/core{repoSuffix}/{variantName}:{imageVersion.ToString(2)}-{os}{arch}";
        }

        public override string ToString()
        {
            return typeof(ImageData).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(propInfo => $"{propInfo.Name}='{propInfo.GetValue(this) ?? "<null>"}'")
                .Aggregate((working, next) => $"{working}, {next}");
        }
    }
}
