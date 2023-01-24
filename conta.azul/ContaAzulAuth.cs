using file.io;
using FileIO;
using Microsoft.Web.WebView2.Core;
using NovoServico;
using NovoServico.outros;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace conta_azul
{




    public partial class ContaAzulAuth : Window
    {

        // nesse site da pra ciar post requests e gerar o codigo em c#
        // https://reqbin.com/req/csharp/zvtstmpb/post-request-examplem

        private readonly String CLIENT_ID = ChavesApi.CLIENT_ID;
        private readonly String CLIENT_SECRET = ChavesApi.CLIENT_SECRET;
        private readonly String REDIRECT_URI = ChavesApi.REDIRECT_URI;



        private readonly String SCOPE = ChavesApi.SCOPE;
        private readonly String STATE = ChavesApi.STATE;

        public ContaAzulAuth()
        {
            InitializeComponent();

        }


        public void autenticar(Action<String?> callback)
        {
            // detecta quando o webview esta pronto pra ser usado
            webView.CoreWebView2InitializationCompleted += obterAutorizacaoDoUsuario;
            void obterAutorizacaoDoUsuario(object? sender, CoreWebView2InitializationCompletedEventArgs e)
            {

                String getCodeUrl = "https://api.contaazul.com/auth/authorize?"
                    + "redirect_uri=" + REDIRECT_URI
                    + "&client_id=" + CLIENT_ID
                    + "&scope=" + SCOPE
                    + "&state=" + STATE;

                webView.Source = new Uri(getCodeUrl);
                webView.CoreWebView2.HistoryChanged += CoreWebView2_HistoryChanged;

                void CoreWebView2_HistoryChanged(object? sender, object e)
                {
                    String currentUri = webView.Source.AbsoluteUri;

                    if (currentUri.StartsWith(REDIRECT_URI)) obterTokenDeAcesso(currentUri.Split("code=")[1].Split("&state")[0], callback);

                }
            }
        }

        private void obterTokenDeAcesso(String code, Action<String?> callback)
        {

            try
            {
                var url = "https://api.contaazul.com/oauth2/token?";

                var httpRequest = (HttpWebRequest)HttpWebRequest.Create(url);
                httpRequest.Method = "POST";

                httpRequest.Headers["Authorization"] = "Basic " + EncodeBase64(CLIENT_ID + ":" + CLIENT_SECRET);
                httpRequest.ContentType = "application/x-www-form-urlencoded";

                var data = "redirect_uri=https%3A%2F%2Fwww.composicao-es.com%2F&code=" + code + "&grant_type=authorization_code";

                using (var streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
                {
                    streamWriter.Write(data);
                }

                var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {

                    if ("OK".Equals(httpResponse.StatusCode.ToString()))
                    {
                        salvarCredenciais(streamReader, httpResponse);
                        callback(null);
                    }
                    else callback(httpResponse.StatusCode.ToString());

                    this.Close();

                }
            }
            catch (Exception ex)
            {

                Debug.WriteLine(":: erro de conexão " + ex.Message);
                this.Close();
                callback(ex.Message);
            }
        }

        public void atualizarTokenDeAcesso(ContaAzulCredModel credenciais, Action<String?> callback)
        {

            try
            {
                var url = "https://api.contaazul.com/oauth2/token";

                var httpRequest = (HttpWebRequest)WebRequest.Create(url);
                httpRequest.Method = "POST";

                httpRequest.Headers["Authorization"] = "Basic " + EncodeBase64(CLIENT_ID + ":" + CLIENT_SECRET);
                httpRequest.ContentType = "application/json";

                String x = credenciais.RefreshToken;
                var data = @"{ ""grant_type"": ""refresh_token"", ""refresh_token"": """ + x + "\"}";
                Debug.WriteLine(":: data: " + data);

                using (var streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
                {
                    streamWriter.Write(data);
                }

                var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    if ("OK".Equals(httpResponse.StatusCode.ToString()))
                    {
                        salvarCredenciais(streamReader, httpResponse);
                        callback(null);
                    }
                    else callback(httpResponse.StatusCode.ToString() + " " + httpResponse.StatusDescription);

                }

                Console.WriteLine(httpResponse.StatusCode + " " + httpResponse.StatusDescription);
            }
            catch (Exception ex)
            {

                Debug.WriteLine(":: erro de conexão " + ex.Message);
                callback(ex.Message);
            }
        }

        private void salvarCredenciais(StreamReader streamReader, HttpWebResponse httpResponse)
        {
            var result = streamReader.ReadToEnd();

            var contaAzulCredenciais = ContaAzulCredModel.FromJson(result);

            Preferencias.inst().save(Preferencias.contaAzulcred, Serialize.ToJson(contaAzulCredenciais));
            Debug.WriteLine("response: " + httpResponse.StatusCode);

        }

        public string EncodeBase64(string value)
        {
            var valueBytes = System.Text.Encoding.ASCII.GetBytes(value);
            return Convert.ToBase64String(valueBytes);
        }

        private void fechandoJanela(object sender, EventArgs e)
        {
            webView.Dispose();
            Debug.WriteLine(":: webView liberou recursos");
        }



    }
}
