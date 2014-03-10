using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MajimoDatabase
{
    internal static class Hilfsfunktionen
    {
        public static string GetTextRessource(string ressourcenName)
        {
            var retval = "";

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "MajimoDatabase." + ressourcenName;
            // var names = assembly.GetManifestResourceNames();

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) throw new Exception(String.Format("Die eingebettete Ressource {0} ist nicht vorhanden", ressourcenName));

                using (var reader = new StreamReader(stream))
                {
                    retval = reader.ReadToEnd();
                }
            }

            return retval;
        }


    }
}
