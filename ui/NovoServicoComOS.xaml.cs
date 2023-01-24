using conta.azul.modelos;
using conta_azul;
using CriadorDePastas.trello;
using FileIO;
using MaterialDesignThemes.Wpf;
using Notification.Wpf;
using outros;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Timer = System.Timers.Timer;
using Card = trello.modelos.TrelloCard;
using domain;

namespace ui
{
    /// <summary>
    /// Lógica interna para NovoServicoComOS.xaml
    /// </summary>
    public partial class NovoServicoComOS : Window
    {
        private OrdemDeVenda? ordemDeVenda = null;
        private Timer? timerDePesquisaOS;
        private TelaPrincipal telaPrincipal;

        public NovoServicoComOS(TelaPrincipal listaDeServicos)
        {
            InitializeComponent();

            tbName.IsEnabled = false;
            tbData.IsEnabled = false;

            tbNumOs.Focus();
            pbar.Visibility = Visibility.Hidden;
            this.telaPrincipal = listaDeServicos;
        }

        private void executarTarefa(object sender, RoutedEventArgs e)
        {

            if (ordemDeVenda != null && tbName.Text.Length > 0 && tbNumOs.Text.Length > 0)
            {
                if (tbData.Text.Length == 10 && Regex.IsMatch(tbData.Text, @"[0-9]{2}/[0-9]{2}/[0-9]{4}"))
                {
                    DateTime dtEntrega = DateTime.Parse(tbData.Text, new CultureInfo("pt-BR"));
                    ordemDeVenda!.dataDeEntrega = dtEntrega;
                    criarServico();
                }
                else UiUtils.erroMsg(this.GetType().Name,"Verifique a data de entrega");

            }
        }

        private void baixarOrdemDeVenda(string numOS)
        {

            Async.runAsync(() =>
            {
                ContaAzulManager.GetInstance().carregarVenda(numOS, atualizarUI);
            });

        }

        private void atualizarUI(string? erro, OrdemDeVenda? venda)
        {
            Async.runOnUI(() =>
            {


                if (erro != null)
                {
                    UiUtils.erroMsg(this.GetType().Name,erro);
                    pbar.IsIndeterminate = false;
                    tbNumOs.IsEnabled = true;
                    tbNumOs.Focus();
                    tbNumOs.SelectAll();
                    lblStatus.Content = "Erro ao encontrar Ordem de venda";
                }
                else
                {

                    ordemDeVenda = venda;
                    tbName.IsEnabled = true;
                    tbName.Text = "(" + venda!.Customer.Name + ")";

                    tbData.IsEnabled = true;
                    
                    tbDesc.Text = ordemDeVenda?.carregarDescricao();
                    stackDescricao.Visibility = Visibility.Visible;


                    String? dataDeEntrega = ordemDeVenda!.carregarDataDeEntregaPrevista();

                    if (dataDeEntrega == null)
                    {
                        tbData.Text = DateTime.Now.ToString("dd/MM/yyyy");
                        ordemDeVenda!.dataDeEntrega = DateTime.Now;
                        lblDataInfo.Content = "° Não foi especificada a data de entrega da venda.";
                    }
                    else tbData.Text = dataDeEntrega;

                    lblDataInfo.Visibility = Visibility.Visible;


                    lblStatus.Content = "Ordem de venda encontrada!";
                    pbar.IsIndeterminate = false;
                    pbar.Value = 33;
                }
            });

        }

        private void criarServico()
        {
            string clientName = Nome.aplicarRegras(tbName.Text);

            lblStatus.Content = "Criando pastas no servidor...";

            Async.runAsync(() => { new Pastas().criarPastaDeServico(clientName, pastaCriada); });


        }

        private void pastaCriada(String? erro, String caminho)
        {
            void runOnUI()
            {

                if (erro != null)
                {
                    UiUtils.erroMsg(this.GetType().Name,erro);
                    pbar.Value = 0;
                    lblStatus.Content = "Falha...";

                    return;
                }

                pbar.Value = 66;
                var nomePasta = System.IO.Path.GetFileName(caminho);
                telaPrincipal.listaDeServicos?.carregarServicos();
                criarCartaoNoTrello(nomePasta, caminho);

            }

            Async.runOnUI(runOnUI);

        }

        private void criarCartaoNoTrello(string nomePasta, string caminho)
        {
            lblStatus.Content = "Criando cartão no Trello...";
            String descricao = tbDesc.Text;
            Async.runAsync(() =>
            {
                new TrelloApi().criarCartaoComOs(nomePasta, ordemDeVenda!,descricao, (string? erro, String? url) =>
                {
                    Async.runOnUI(() =>
                    {
                        if (erro != null) UiUtils.erroMsg(this.GetType().Name,erro);
                        else
                        {
                            pbar.Value = 99;
                            lblStatus.Content = "Sucesso!";
                            telaPrincipal.webViewTrello.Source = new Uri(url!);
                        }

                        Async.runOnUI(800, () =>
                        {
                            this.Close();
                            // abrirPastaDoServico(caminho);
                        });
                    });

                });

            });

        }

        private void abrirPastaDoServico(String caminho)
        {
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

        private void tbNumOs_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (timerDePesquisaOS != null)
            {
                timerDePesquisaOS.Stop();
                timerDePesquisaOS = null;
            }

            timerDePesquisaOS = new Timer();
            timerDePesquisaOS.Interval = 500;
            timerDePesquisaOS.Elapsed += (object? sender, ElapsedEventArgs e) =>
            {
                timerDePesquisaOS.Stop();
                Async.runOnUI(() =>
                {
                    if (ordemDeVenda == null && tbNumOs.Text.Length > 0)
                    {
                        pbar.Visibility = Visibility.Visible;
                        pbar.IsIndeterminate = true;
                        lblStatus.Content = "Buscando ordem de venda no Conta Azul...";
                        tbNumOs.IsEnabled = false;
                        var numOS = new Regex("[^0-9]").Replace(tbNumOs.Text, "");
                        tbNumOs.Text = numOS;
                        baixarOrdemDeVenda(numOS);
                    }
                });
            };
            timerDePesquisaOS.Start();


        }

    }

}
