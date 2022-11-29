using file.io;
using Microsoft.Web.WebView2.Core;
using NovoServico.outros;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static System.Formats.Asn1.AsnWriter;

namespace trello
{


    public partial class TrelloAuth : Window
    {

        public static String KEY = "bcc95e893773d21bc898224f675f0d7c";
        public static String OAUTH_SECRET = "258cfa420ce3ed5507638be400043bf891f47b89aa0e6a029334075ab6805c92";
        private readonly String REDIRECT_URI = "https://www.composicao-es.com/";

        public TrelloAuth()
        {
            InitializeComponent();
        }

        public void autenticar(Action callback)
        {
            // detecta quando o webview esta pronto pra ser usado
            webView.CoreWebView2InitializationCompleted += obterAutorizacaoDoUsuario;
            void obterAutorizacaoDoUsuario(object? sender, CoreWebView2InitializationCompletedEventArgs e)
            {
                //=&=&=&=&=" + KEY

                String getCodeUrl = "https://trello.com/1/authorize?"
                    + "expiration=" + "never"
                    + "&scope=" + "read,write,account"
                    + "&response_type=" + "token"
                    + "&name=" + "Novo Serviço (Composição)"
                    + "&return_url=" + REDIRECT_URI
                    + "&key=" + KEY;

                webView.Source = new Uri(getCodeUrl);
                webView.CoreWebView2.HistoryChanged += CoreWebView2_HistoryChanged;

                void CoreWebView2_HistoryChanged(object? sender, object e)
                {
                    String currentUri = webView.Source.AbsoluteUri;

                    if (currentUri.StartsWith(REDIRECT_URI)) salvarToken(currentUri.Split("#token=")[1], callback);

                }
            }
        }

        private void salvarToken(String token, Action callback)
        {
            Preferencias.inst().save(Preferencias.trelloUserToken, token);
            callback.Invoke();
        }

        private void fechandoJanela(object sender, EventArgs e)
        {
            webView.Dispose();
            Debug.WriteLine(":: webView liberou recursos");
        }
    }
}

