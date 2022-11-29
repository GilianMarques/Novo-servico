using conta.azul.modelos;
using CriadorDePastas.trello;
using outros;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection.Emit;
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
using trello.modelos;
using Label = trello.modelos.Label;
using Path = System.IO.Path;

namespace ui
{

    //https://stackoverflow.com/a/66004049

    /*
     obter ordem de venda

     baixar o cartaoPrincipal no trello pra obter a id

     atualizar cartaoPrincipal no trello e remover etiqueta sem os
    
     */

    /// <summary>
    /// Lógica interna para AnexarOS.xaml
    /// </summary>
    public partial class AnexarOS : Window
    {
        private string caminhoServico;
        private readonly string nomeServico;
        private OrdemDeVenda? ordemDeVenda = null;
        private Timer? timerDePesquisaOS;
        private TrelloCard? cartao;

        public AnexarOS(string caminhoServico)
        {
            InitializeComponent();
            this.caminhoServico = caminhoServico;
            nomeServico = Path.GetFileName(caminhoServico);
            lblBlockNomeServico.Text = nomeServico;

            fazerChecagensPreOperacao();
        }

        private void fazerChecagensPreOperacao()
        {

            cpConclude.IsEnabled = false;
            tbNumOs.IsEnabled = false;


            Async.runAsync(() =>
            {
                atualizarInfo("Verificando cartão do serviço...");
                new TrelloApi().obterCartaoPorNome(Path.GetFileName(caminhoServico), (string? erro, TrelloCard? cartao) =>
                {
                    if (erro != null)
                    {
                        UiUtils.erroMsg(this.GetType().Name,"Erro buscando pelo cartão do serviço no Trello: " + erro);
                        atualizarInfo("O cartão pode existir ou não...", false, true);
                    }
                    else if (cartao == null) { UiUtils.erroMsg(this.GetType().Name,"O cartão do serviço não foi encontrado..."); Async.runOnUI(() => { this.Close(); }); }
                    else { atualizarInfo("Tudo pronto!", false, true); this.cartao = cartao; }
                });
            });

            void atualizarInfo(String info = "", bool indeterminate = true, bool cpConcludeHab = false)
            {
                Async.runOnUI(() =>
                {
                    lblStatus.Content = info;
                    pbar.IsIndeterminate = indeterminate;
                    cpConclude.IsEnabled = cpConcludeHab;
                    tbNumOs.IsEnabled = cpConcludeHab;
                    if (tbNumOs.IsEnabled) tbNumOs.Focus();
                });
            }

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

        private void baixarOrdemDeVenda(string numOS)
        {

            Async.runAsync(() =>
            {
                ContaAzulManager.GetInstance().carregarVenda(numOS, atualizarUIOrdemDeVenda);
            });

        }

        private void atualizarUIOrdemDeVenda(string? erro, OrdemDeVenda? venda)
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
                    tbData.IsEnabled = true;
                    cpConclude.IsEnabled = true;

                    lblStatus.Content = "Ordem de venda encontrada!";
                    pbar.IsIndeterminate = false;
                    pbar.Value = 33;

                    String? dataDeEntrega = ordemDeVenda!.carregarDataDeEntregaPrevista();

                    if (dataDeEntrega == null) tbData.Text = DateTime.Now.ToString("dd/MM/yyyy");
                    else tbData.Text = dataDeEntrega;

                }

            });

        }

        private void executarTarefa(object sender, RoutedEventArgs e)
        {

            if (tbData.Text.Length == 10 && Regex.IsMatch(tbData.Text, @"[0-9]{2}/[0-9]{2}/[0-9]{4}"))
            {
                if (cartao != null) atualizarCartaoNoTrello();
                else baixarCartaoNoTrello();
            }
            else UiUtils.erroMsg(this.GetType().Name,"Verifique a data de entrega");


        }

        private void baixarCartaoNoTrello()
        {
            lblStatus.Content = "Baixando cartão...";
            pbar.IsIndeterminate = true;

            Async.runAsync(() =>
            {
                new TrelloApi().obterCartaoPorNome(nomeServico, (String? erro, TrelloCard? cartao) =>
                {
                    Async.runOnUI(() =>
                    {
                        if (erro != null)
                        {
                            UiUtils.erroMsg(this.GetType().Name,erro);
                            lblStatus.Content = "Erro localizando o cartão do serviço.";
                            pbar.IsIndeterminate = false;
                            pbar.Value = 0;
                        }
                        else if (cartao != null)
                        {
                            this.cartao = cartao;
                            atualizarCartaoNoTrello();
                        }
                        else
                        {
                            lblStatus.Content = "Não existe um cartão associado a esse serviço para anexar a OS";
                            pbar.IsIndeterminate = false;
                            pbar.Value = 0;
                        }
                    });



                });
            });


        }

        private void atualizarCartaoNoTrello()
        {


            lblStatus.Content = "Atualizando cartão...";
            pbar.IsIndeterminate = true;
            // atualizar descrição
            cartao!.Desc = ordemDeVenda!.carregarDescricao();

            // data de entrega
            cartao.Due = DateTime.Parse(tbData.Text, new CultureInfo("pt-BR"));

            Async.runAsync(() =>
            {
                new TrelloApi().anexarOsAoCartao(cartao, atualizarUICartaoAtualizado);
            });
        }

        private void atualizarUICartaoAtualizado(string? erro)
        {
            Async.runOnUI(() =>
            {
                pbar.IsIndeterminate = false;
                if (erro != null)
                {
                    UiUtils.erroMsg(this.GetType().Name,erro);
                    pbar.Value = 0;
                    lblStatus.Content = "Erro atualizando cartão.";
                }
                else
                {
                    pbar.Value = 100;
                    lblStatus.Content = "Sucesso!";
                    Async.runOnUI(1000, () =>
                    {
                        this.Close();
                    });

                }
            });
        }
    }
}
