using conta.azul.modelos;
using CriadorDePastas.trello;
using domain;
using FileIO;
using outros;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace ui
{
    /// <summary>
    /// Lógica interna para NovoServicoSemOs.xaml
    /// </summary>
    public partial class NovoServicoSemOs : Window
    {
        private TelaPrincipal telaPrincipal;

        public NovoServicoSemOs(TelaPrincipal listaDeServicos)
        {
            InitializeComponent();
            tbName.Focus();
            this.telaPrincipal = listaDeServicos;
        }

        private void executarTarefa(object sender, RoutedEventArgs e)
        {
            if (tbName.Text.Length > 0)
            {
                pbar.IsIndeterminate = true;
                criarServico();
            }
        }

        private void criarServico()
        {
            string clientName = Nome.aplicarRegras(tbName.Text);

            Async.runAsync(action);
            void action()
            {
                new Pastas().criarPastaDeServico(clientName, pastaCriada);
            }
        }

        private void pastaCriada(String? erro, String caminho)
        {

            Async.runOnUI(() =>
            {
                if (erro != null) UiUtils.erroMsg(this.GetType().Name, erro);
                else
                {

                    telaPrincipal.listaDeServicos?.carregarServicos();
                    if ((bool)cbAddToTrello2.IsChecked!) criarCartaoNoTrello(Path.GetFileName(caminho), caminho);
                    else animarEsair(caminho);
                }
            });

        }

        private void criarCartaoNoTrello(string nomePasta, string caminho)
        {


            Async.runAsync(() =>
            {
                new TrelloApi().criarCartao(nomePasta, (string? erro, String? url) =>
                {
                    Async.runOnUI(() =>
                    {
                        if (erro != null) UiUtils.erroMsg(this.GetType().Name, erro);
                        else telaPrincipal.webViewTrello.Source = new Uri(url!);

                        animarEsair(caminho);

                    });
                });
            });

        }

        private void animarEsair(string caminho)
        {

            pbar.IsIndeterminate = false;
            pbar.Value = 0;

            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler((object? sender, EventArgs e) =>
            {
                pbar.Value += 10;
                if (pbar.Value == 100)
                {
                    dispatcherTimer.Stop();

                    Async.runOnUI(800, () =>
                    {
                        this.Close();
                        abrirPastaDoServico(caminho);
                    });

                }
            });
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            dispatcherTimer.Start();


        }

        private void abrirPastaDoServico(String caminho)
        {
            if (cbAddToTrello2.IsChecked == true) return;
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = caminho,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception) { UiUtils.erroNot(String.Format("Não foi possível abrir a pasta {0}", caminho)); }
        }

    }
}
