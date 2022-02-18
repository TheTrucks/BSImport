﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BSImport
{
    public static class CacheManager
    {
        static CacheManager()
        {
            ReadWriting = false;
            CachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache.ff");
        }

        private static string CachePath;
        public static DateTime LastDateTime
        {
            get
            {
                if (File.Exists(CachePath))
                {
                    using (var CacheFile = File.Open(CachePath, FileMode.Open))
                        using (var SR = new StreamReader(CacheFile, Encoding.UTF8))
                            return DateTime.ParseExact(SR.ReadLine().Trim(), "yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                    return DateTime.UtcNow.AddHours(-2);
            }
            set
            {
                using (var CacheFile = File.Open(CachePath, FileMode.Create))
                    using (var SW = new StreamWriter(CacheFile, Encoding.UTF8))
                        SW.WriteLine(value.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture));
            }
        }
    }
}
