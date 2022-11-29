using CriadorDePastas.trello;
using file.io;
using FileIO;
using outros;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using trello;
using trello.modelos;
using TrelloCard = trello.modelos.TrelloCard;

namespace ui
{
    /// <summary>
    /// Lógica interna para CriarParcial.xaml
    /// </summary>
    public partial class CriarParcial : Window
    {
        private TrelloApi trelloApi = new TrelloApi();

        private TrelloCard cartaoPrincipal = new TrelloCard();
        private String[] itensParciais = new String[0];

        private int numeroServico;
        private readonly string nomeServico;

        private bool cartaoPrincipalMovido = false;

        public CriarParcial(string nomeServico)
        {
            InitializeComponent();
            this.nomeServico = nomeServico;

            parentItens.Visibility = Visibility.Collapsed;
            parentTbNome.Visibility = Visibility.Collapsed;
            cpConclude.Visibility = Visibility.Collapsed;

            pbar.IsIndeterminate = true;
            lblBlockNomeServico.Text = nomeServico;
            lblStatus.Content = "Buscando cartão no Trello...";

            Async.runAsync(() => { baixarCartao(); });

        }

        private void baixarCartao() => new TrelloApi().obterCartaoPorNome(nomeServico, (String? erro, TrelloCard? cartao) =>
            {
                Async.runOnUI(() =>
                {
                    lblStatus.Content = "Erro.";

                    if (erro != null) UiUtils.erroMsg(this.GetType().Name, "Erro buscando pelo cartão: " + erro);
                    else if (cartao == null) UiUtils.erroMsg(this.GetType().Name, "Cartão não existe.");
                    else
                    {
                        this.cartaoPrincipal = cartao;
                        carregarItens();
                    }
                });

            });

        private void carregarItens()
        {
            lblStatus.Content = "Carregando itens...";

            var r = new Regex("[*]{2}[ITEM]{4}[ ][0-9]+[*]{2}"); //**ITEM 1**
            var itens = r.Matches(cartaoPrincipal.Desc).Count;

            if (itens > 0) // pode ser q na desc do cartao nao tenham itens (como quando a OS nao for fornecida por exemplo)
            {
                itensParciais = new string[itens + 1];
                for (int i = 0; i < itens; i++)
                {
                    CheckBox cb = new CheckBox();
                    cb.Content = $"ITEM {i + 1}";
                    cb.Padding = new Thickness(8);
                    cb.Checked += (object sender, RoutedEventArgs e) =>
                    {
                        itensParciais[i] = cb.Content.ToString()!;
                        Debug.WriteLine($":: add na lista {cb.Content} -> {itensParciais[i]} -> {itensParciais.Length} ");
                    };
                    cb.Unchecked += (object sender, RoutedEventArgs e) =>
                    {
                        itensParciais[i] = null;
                        Debug.WriteLine($":: rem na lista {cb.Content} -> {itensParciais[i]} -> {itensParciais.Length} ");
                    };

                    containerItens.Children.Add(cb);
                }

                lblStatus.Content = "Selecione os itens desejados.";
                parentItens.Visibility = Visibility.Visible;
            }
            else
            {
                itensParciais = new string[1];
                lblStatus.Content = "Nenhum item encontrado.";
                parentTbNome.Visibility = Visibility.Visible;
                tbName.Focus();
            }

            cpConclude.Visibility = Visibility.Visible;
            pbar.IsIndeterminate = false;
        }

        private void executarTarefa(object sender, RoutedEventArgs e)
        {
            var nomeItem = tbName.Text.ToUpper().Trim();

            if (nomeItem.Length > 0) itensParciais[0] = nomeItem;

            numeroServico = new Pastas().obterNumero(nomeServico);

            lblStatus.Content = "Baixando anexos do cartão...";
            pbar.IsIndeterminate = true;
          
            Debug.WriteLine($":: cartoes {String.Join(",", itensParciais)}");

            Async.runAsync(() => { baixarESelecionarAnexos(); });

        }
        /*
         :: add na lista ITEM 1 -> ITEM 1 -> 10 
:: add na lista ITEM 2 -> ITEM 2 -> 10 
:: add na lista ITEM 3 -> ITEM 3 -> 10 
:: add na lista ITEM 4 -> ITEM 4 -> 10 
:: add na lista ITEM 5 -> ITEM 5 -> 10 
:: add na lista ITEM 6 -> ITEM 6 -> 10 
:: add na lista ITEM 7 -> ITEM 7 -> 10 
:: add na lista ITEM 8 -> ITEM 8 -> 10 
:: add na lista ITEM 9 -> ITEM 9 -> 10 
:: rem na lista ITEM 9 ->  -> 10 
:: add na lista ITEM 9 -> ITEM 9 -> 10 
:: cartoes ,,,,,,,,,ITEM 9

         */
        private void baixarESelecionarAnexos() => trelloApi.baixarAnexos(cartaoPrincipal.Id, (String? erro, List<Anexo> anexos) =>
    {

        if (erro != null) Async.runOnUI(() =>
        {
            lblStatus.Content = "Erro.";
            pbar.IsIndeterminate = false;
            UiUtils.erroMsg(this.GetType().Name, erro);
            return;
        });



        for (int x = 0; x < itensParciais.Length; x++)
        {
            if (itensParciais[x] == null) continue;

            Anexo? capa = null;

            // busca por uma capa de nome identico ao item selecionado
            for (int i = 0; i < anexos.Count; i++) if (anexos[i].Name!.Equals(itensParciais[x])) { capa = anexos[i]; break; }

            // busca por uma capa de nome parecido ao item selecionado se a capa de nome identico nao for encontrado
            if (capa == null) for (int i = 0; i < anexos.Count; i++) if (anexos[i].Name!.ToLower().StartsWith(itensParciais[x]!.ToLower())) capa = anexos[i];

            cariarCartaoParcial($"{numeroServico} - {itensParciais[x]}", capa);

        }

        sair();

    });

        private void cariarCartaoParcial(string nomeServico, Anexo? capa)
        {

            Async.runOnUI(() => { lblStatus.Content = $"Criando cartão parcial '{nomeServico}'..."; });

            var resultado = trelloApi.criarCartaoParcial(cartaoPrincipal, nomeServico);

            if (resultado.erro != null) UiUtils.erroMsg(this.GetType().Name, $"Erro criando parcial '{nomeServico}', causa: {resultado.erro}");
            else if (capa != null) anexarCapa(resultado.valor!, capa);
            else conectarCartoes(resultado.valor!);

        }

        private void anexarCapa(TrelloCard cartaoParcial, Anexo capa)
        {
            Async.runOnUI(() => { lblStatus.Content = "Anexando capa..."; });

            var resultado = trelloApi.anexarCapaEmCartaoParcial(cartaoParcial, capa.Url!);
            if (resultado.erro != null) UiUtils.erroMsg(this.GetType().Name, $"Erro ao anexar capa no cartão parcial ({cartaoParcial.Name}). Anexe manualmente usando o Trello.\nCausa do erro: {resultado.erro}");
            conectarCartoes(cartaoParcial);
        }

        private void conectarCartoes(TrelloCard cartaoParcial)
        {
            Async.runOnUI(() => { lblStatus.Content = $"Conectando cartão parcial ({cartaoParcial.Name}) ao principal..."; });


            var resultado = trelloApi.anexarUrlAoCartao(cartaoParcial.Url, cartaoPrincipal.Id);
            if (resultado.erro != null) UiUtils.erroMsg(this.GetType().Name, $"Erro ao anexar cartão parcial ao cartão principal. Anexe manualmente usando o Trello.\nO processo segue em execução, caso hajam mais erros eles serão exibidos aqui\nCausa do erro: {resultado.erro}");

            Async.runOnUI(() => { lblStatus.Content = $"Conectando cartão principal ao parcial ({cartaoParcial.Name})..."; });

            var resultado2 = trelloApi.anexarUrlAoCartao(cartaoPrincipal.Url, cartaoParcial.Id);
            if (resultado2.erro != null) UiUtils.erroMsg(this.GetType().Name, $"Erro ao anexar cartão principal ao cartão parcial. Anexe manualmente usando o Trello.\nCausa do erro: {resultado2.erro}");

            moverCartaoPrincipalParaListaEmAndamento();

        }

        private void moverCartaoPrincipalParaListaEmAndamento()
        {
            //quando houver mais de um cartao parcila pra ser criado me certifico de nao mover o cartao principal mais de uma vez atoa
            if (cartaoPrincipalMovido) return;

            Async.runOnUI(() => { lblStatus.Content = "Movendo cartão principal para lista em andamento..."; });

            var resultado = trelloApi.moverCartaoParaLista(cartaoPrincipal.Id, TrelloApi.listaEmAndamento);

            if (resultado.erro != null) UiUtils.erroMsg(this.GetType().Name, $"Erro movendo o cartão principal. Mova manualmente no Trello.\nCausa: {resultado.erro}");
            else cartaoPrincipalMovido = true;

        }

        private void sair()
        {
            Async.runOnUI(() =>
            {
                lblStatus.Content = "Saindo...";
                Async.runOnUI(500, () => { this.Close(); });

            });
        }
    }
}
