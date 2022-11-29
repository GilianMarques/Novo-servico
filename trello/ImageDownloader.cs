using CriadorDePastas.trello;
using outros;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace trello
{
    internal class ImageDownloader
    {

        public Resultado<String> download(String key, string? userToken, Uri capaUrl)
        {

            using (WebClient client = new WebClient())
            {
                var resultado = new Resultado<String>();

                try
                {

                    var path = $"c:\\temp\\{new Random().Next()}.png";

                    var x = new WebHeaderCollection();
                    x.Add("Authorization", $"OAuth oauth_consumer_key=\"{key}\", oauth_token=\"{userToken}\"");

                    client.Headers = x;

                    client.DownloadFile(capaUrl, path);

                    resultado.valor = path;
                    return resultado;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("::erro baixando img " + e.Message);
                    resultado.erro = $"Erro baixando anexo. Causa: {e.Message}";
                    return resultado;
                }
            }
        }



    }
}
