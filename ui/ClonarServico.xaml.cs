using conta.azul.modelos;
using CriadorDePastas.trello;
using FileIO;
using outros;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;
using Path = System.IO.Path;

namespace ui
{
    /// <summary>
    /// Lógica interna para ClonarServico.xaml
    /// </summary>
    public partial class ClonarServico : Window
    {
        private string caminhoServico;
        private string nomeServico;
        private string novoNomeServico;
        private Action atualizarLista;
        private string? caminhoServidor;
        private int numeroServico;
        private string caminhoParentServico;
        private OrdemDeVenda? ordemDeVenda;
        private int novoNumeroDisponivel;
        private string novoCaminhoServico;
        private TelaPrincipal telaPrincipal;
        private Timer timerDePesquisaOS;
        private Pastas pastas = new Pastas();

        public ClonarServico(TelaPrincipal telaPrincipal, string caminhoServico, string nomeServico, Action atualizarLista)
        {
            InitializeComponent();

            this.telaPrincipal = telaPrincipal;
            this.caminhoServico = caminhoServico;
            this.nomeServico = nomeServico;
            this.atualizarLista = atualizarLista;
            this.caminhoServidor = pastas.lerCaminhoDoServidor();
            numeroServico = pastas.obterNumero(nomeServico);
            this.caminhoParentServico = Directory.GetParent(caminhoServico)!.FullName;// o serviço pode estar na raiz do servidor ou em serviços feitos

            tbNumOs.Focus();
            lblBlockNomeServico.Text = nomeServico;
            tbData.IsEnabled = false;

            /*
             
             testes com 2990
             */

            /* por algum motivo, quando a pasta do serviço esta dentro de serviços feitos, o webbrowser nao consegue abrir 
                             parece que é um problema com a quantidade de '//' (barras) no caminho, a solução foi fazer essa verificação e 
                             ajustar o path manualmente
                              */
            //  var array = String.Join("\\", servicos[0].Split("\\", StringSplitOptions.RemoveEmptyEntries).ToList());

            //  caminhoDoServicoAtual = new Uri(array);



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
                    if (tbNumOs.Text.Length > 0)
                    {
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

            if (tbNumOs.Text.Length > 0) cpConclude.Visibility = Visibility.Collapsed;

            else cpConclude.Visibility = Visibility.Visible;



        }

        private void executarTarefa(object sender, RoutedEventArgs e)
        {
            if (ordemDeVenda != null)
            {

                if (tbData.Text.Length == 10 && Regex.IsMatch(tbData.Text, @"[0-9]{2}/[0-9]{2}/[0-9]{4}"))
                {
                    DateTime dtEntrega = DateTime.Parse(tbData.Text, new CultureInfo("pt-BR"));
                    ordemDeVenda!.dataDeEntrega = dtEntrega;
                    tbData.IsEnabled = false;
                    Async.runAsync(() => { obterNumeroDisponivel(); });
                }
                else UiUtils.erroMsg(this.GetType().Name,"Verifique a data de entrega");

            }
            else Async.runAsync(() => { obterNumeroDisponivel(); });

        }

        private void baixarOrdemDeVenda(string numOS)
        {

            Async.runAsync(() => { ContaAzulManager.GetInstance().carregarVenda(numOS, atualizarUI); });

            void atualizarUI(string? erro, OrdemDeVenda? venda)
            {
                Async.runOnUI(() =>
                {

                    pbar.IsIndeterminate = false;
                    tbNumOs.IsEnabled = true;
                    cpConclude.Visibility = Visibility.Visible;

                    if (erro != null)
                    {
                        UiUtils.erroMsg(this.GetType().Name,erro);
                        tbNumOs.Focus();
                        tbNumOs.SelectAll();
                        lblStatus.Content = "Erro ao encontrar Ordem de venda.";
                    }
                    else if (venda == null) lblStatus.Content = "Não existe ordem de venda com esse número.";
                    else
                    {
                        tbNumOs.IsEnabled = false;
                        ordemDeVenda = venda;
                        cpConclude.Content = " Clonar";
                        lblStatus.Content = $"Ordem em nome de {venda.Customer.Name} encontrada!";

                        ajustarDataDeEntrega();
                    }

                });

            }
        }

        private void ajustarDataDeEntrega()
        {
            tbData.IsEnabled = true;

            String? dataDeEntrega = ordemDeVenda!.carregarDataDeEntregaPrevista();

            if (dataDeEntrega == null)
            {
                tbData.Text = DateTime.Now.ToString("dd/MM/yyyy");
                ordemDeVenda!.dataDeEntrega = DateTime.Now;
                lblDataInfo.Content = "° Não foi especificada a data de entrega da venda.";
            }
            else tbData.Text = dataDeEntrega;

            lblDataInfo.Visibility = Visibility.Visible;

        }

        private void obterNumeroDisponivel()
        {
            Async.runOnUI(() => { lblStatus.Content = "Buscando novo número de serviço disponível..."; pbar.IsIndeterminate = true; });
            novoNumeroDisponivel = pastas.obterProximoNumeroDisponivel();
            clonarServico();
        }

        private void clonarServico()
        {
            novoNomeServico = nomeServico.Replace(numeroServico + "", novoNumeroDisponivel + "");
            novoCaminhoServico = caminhoServidor + novoNomeServico;

            if (copiarSubPastas() && copiarArquivos()) Async.runOnUI(() => { atualizarLista(); criarCartaoNoTrello(); });
            else Async.runOnUI(() =>
            {
                pbar.IsIndeterminate = false;
                pbar.Value = 0;
                lblStatus.Content = "Erro.";
            });


        }

        private bool copiarSubPastas()
        {
            try
            {
                var pastas = Directory.GetDirectories(caminhoServico, "*", SearchOption.AllDirectories);
                foreach (var path in pastas)
                {
                    var caminhoSubPasta = path.Replace("\\\\", "\\");
                    var novoCaminhoSubPasta = caminhoSubPasta.Replace(caminhoParentServico, caminhoServidor).Replace(numeroServico + "", "" + novoNumeroDisponivel); ;

                    Directory.CreateDirectory(novoCaminhoSubPasta);
                    atualizarProgresso(Path.GetFileName(caminhoSubPasta)); // o erro pode estar aqui Replace(caminhoParentServico linha de cima

                }
                return true;
            }
            catch (Exception e)
            {
                UiUtils.erroMsg(this.GetType().Name,$"Erro copiando pastas: '{e.Message}'{removerPastaNova()}");
                return false;
            }
        }

        private bool copiarArquivos()
        {
            try
            {
                var arquivos = Directory.GetFiles(caminhoServico, "*", SearchOption.AllDirectories);
                foreach (var path in arquivos)
                {
                    var caminhoArquivo = path.Replace("\\\\", "\\");
                    var novoCaminhoArquivo = caminhoArquivo.Replace(caminhoParentServico, caminhoServidor).Replace(numeroServico + "", "" + novoNumeroDisponivel); ;

                    new FileInfo(novoCaminhoArquivo).Directory!.Create(); // cria qqer pasta no caminho pro arquivo no caso da pasta de serviço nao ter nenhuma subpasta, apenas arquivos

                    File.Copy(caminhoArquivo, novoCaminhoArquivo, true);
                    atualizarProgresso(Path.GetFileName(caminhoArquivo));
                }
                return true;
            }
            catch (Exception e)
            {
                UiUtils.erroMsg(this.GetType().Name,$"Erro copiando arquivo: '{e.Message}'{removerPastaNova()}");
                return false;
            }
        }

        private void criarCartaoNoTrello()
        {
            lblStatus.Content = "Criando cartão no Trello...";

            Async.runAsync(() =>
            {
                if (ordemDeVenda != null) { new TrelloApi().criarCartaoComOs(nomeServico.Replace(numeroServico + "", novoNumeroDisponivel + ""), ordemDeVenda,ordemDeVenda.carregarDescricao(), callback); }
                else new TrelloApi().criarCartao(nomeServico.Replace(numeroServico + "", novoNumeroDisponivel + ""), callback);

                void callback(string? erro, String? url) => Async.runOnUI(() =>
                    {
                        if (erro != null) UiUtils.erroMsg(this.GetType().Name,erro);
                        else
                        {
                            pbar.Value = 100;
                            pbar.IsIndeterminate = false;
                            lblStatus.Content = "Sucesso!";
                            telaPrincipal.webViewTrello.Source = new Uri(url!);
                        }

                        Async.runOnUI(800, () => { this.Close(); });
                    });
            });

        }

        private void atualizarProgresso(string nomeArquivo) => Async.runOnUI(() => { lblStatus.Content = "Copiando " + nomeArquivo + "..."; });

        private string removerPastaNova()
        {
            try
            {
                Directory.Delete(novoCaminhoServico!, true);
                return "\n\n A operação foi desfeita. Verifique a causa do problema e tente novamente.";
            }
            catch (Exception e)
            {

                return "\n\n Não foi possivel desfazer a operação, apague manualmente a nova pasta e tente novamente.\n\nCausa: " + e.Message;
            }
        }

    }
}
