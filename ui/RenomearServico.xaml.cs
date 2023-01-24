using CriadorDePastas.trello;
using FileIO;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using Microsoft.WindowsAPICodePack.Shell;
using outros;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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
using trello.modelos;
using Windows.Storage;
using Path = System.IO.Path;
using SystemProperties = Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties;
using System.Collections;
using domain;

namespace ui
{
    /// <summary>
    /// Lógica interna para RenomearServico.xaml
    /// </summary>
    public partial class RenomearServico : Window
    {

        private string caminhoParentServico; //W:\# Gilian\SOFTWARE\SERVIDOR DE TESTES GILIAN
        private string caminhoServico; //W:\# Gilian\SOFTWARE\SERVIDOR DE TESTES GILIAN\SERVIÇO 4208 - PREVENT PHARMA
        private string nomeServico; //SERVIÇO 4208 - PREVENT PHARMA
        private string nomeCliente; //PREVENT PHARMA

        private readonly Action carregarServicos;

        private string? novoCaminhoServico;
        private string? novoNomeCliente;

        private int arquivosAcopiar;
        private int arquivosCopiados;

        private TrelloCard? cartao;

        public RenomearServico(string caminhoServico, string nomeServico, Action carregarServicos)
        {
            this.caminhoServico = caminhoServico;
            this.nomeServico = nomeServico;
            this.caminhoParentServico = caminhoServico.Replace(nomeServico, "");
            this.carregarServicos = carregarServicos;
            this.nomeCliente = new Regex("[SERVIÇO]{7}[ ][0-9]+[ ][-][ ]").Split(nomeServico)[1];

            InitializeComponent();

            tbName.Text = nomeCliente;
            tbName.Focus();

            // pra informar antes de perder tempo escrevendo o novo nome
            fazerChecagensPreOperacao();
        }

        /// <summary>
        /// Verifica se os arquivos podem ser manuseados e procura pelo cartaoPrincipal do serviço no trello pra ajudar o usuario a saber se ta tudo certo pra renomear o serviço
        /// </summary>
        private void fazerChecagensPreOperacao()
        {
            void atualizarInfo(String info = "", bool indeterminate = true, bool cpConcludeHab = false)
            {
                Async.runOnUI(() =>
                {
                    lblStatus.Content = info;
                    pbar.IsIndeterminate = indeterminate;
                    cpConclude.IsEnabled = cpConcludeHab;
                });
            }

            Async.runAsync(() =>
            {
                atualizarInfo("Aguarde: Verificando arquivos...");
                var usos = verificarSePodeOperar();
                if (usos.Count > 0)
                {
                    fechar(0, false);
                    UiUtils.erroMsg(this.GetType().Name, "Feche os seguintes arquivos para prosseguir:\n\n" + String.Join("\n", usos.ToArray()));
                }
                else
                {

                    atualizarInfo("Aguarde: Verificando cartão do serviço...");
                    new TrelloApi().obterCartaoPorNome(Path.GetFileName(caminhoServico), (string? erro, TrelloCard? cartao) =>
                    {
                        if (erro != null)
                        {
                            UiUtils.erroMsg(this.GetType().Name, "Erro buscando pelo cartão do serviço no Trello: " + erro);
                            atualizarInfo("O cartão pode existir ou não...", false, true);
                        }
                        else if (cartao == null) { atualizarInfo("O cartão do serviço não foi encontrado...", false, true); UiUtils.erroMsg(this.GetType().Name, "O cartão do serviço não foi encontrado!"); }
                        else { atualizarInfo("Tudo pronto!", false, true); this.cartao = cartao; }
                    });
                }
            });


        }

        private void executarTarefa(object sender, RoutedEventArgs e)
        {
            if (tbName.Text.Length > 0 && tbName.Text != nomeCliente)
            {

                novoNomeCliente = Nome.aplicarRegras(tbName.Text);

                novoCaminhoServico = Directory.GetParent(caminhoServico)!.FullName + Path.DirectorySeparatorChar + nomeServico.Replace(nomeCliente, novoNomeCliente);

                Async.runAsync(() => { renomearServico(); });
            }
            else UiUtils.erroMsg(this.GetType().Name, "Verifique o nome.");
        }

        /// <summary>
        /// A ideia aqui é fazer uma copia da pasta e seus arquivo ja com o novo nome e tentar remover a pasta antiga, se nao conseguir avisar o usuario pra remover manualmente, é mais seguro assim
        /// </summary>
        /// <param name="novoCaminhoServico"></param>
        private void renomearServico()
        {
            Async.runOnUI(() =>
            {
                lblStatus.Content = "Copiando novos arquivos...";
            });

            var usos = verificarSePodeOperar();
            if (usos.Count > 0)
            {
                UiUtils.erroMsg(this.GetType().Name, "Não é possível renomear porque os seguintes arquivos estão em uso:\n" + String.Join("\n", usos.ToArray()));
                Async.runOnUI(() =>
                {
                    pbar.Value = 0;
                    lblStatus.Content = "Arquivos abertos nao podem ser renomeados.";
                });
                return;
            }

            if (criarNovaPasta() && copiarSubPastas() && copiarArquivos())
            {
                removerPastaAntiga();
                Async.runOnUI(() =>
                {
                    if (cartao == null) buscarCartaoNoTrello();
                    else atualizarCartaoNoTrello();
                });
            }
            else
            {
                Async.runOnUI(() =>
                {
                    pbar.Value = 0;
                    lblStatus.Content = "Erro.";
                });
            }
        }

        private List<String> verificarSePodeOperar()
        {
            //esta funçao deve ser executada na mainThread pq altera o arquivosAcopiar que se for zero quando começar a açao de cópia vai dar erro na hora de atualizar a barra de progresso
            arquivosAcopiar = 0;
            List<String> lista = new List<String>();

            foreach (var arquivo in Directory.GetFiles(caminhoServico, "*", SearchOption.AllDirectories))
            {
                if (arquivoTravado(arquivo)) lista.Add(Path.GetFileName(arquivo));
                arquivosAcopiar++;
            }

            arquivosAcopiar += Directory.GetDirectories(caminhoServico, "*", SearchOption.AllDirectories).Length;

            return lista;

        }

        protected virtual bool arquivoTravado(String caminho)
        {

            try
            {

                var nomeArquivo = Path.GetFileName(caminho);
                var novoCaminho = caminho.Replace(nomeArquivo, nomeArquivo + "_" + Path.GetRandomFileName());
                System.IO.File.Move(caminho, novoCaminho);
                //volto ao nome original do arquivo
                System.IO.File.Move(novoCaminho, caminho);

                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }

        private bool criarNovaPasta()
        {
            try
            {
                Directory.CreateDirectory(novoCaminhoServico!);
                return true;
            }
            catch (Exception e)
            {
                UiUtils.erroMsg(this.GetType().Name, "Erro na pasta raiz: " + caminhoServico + ". Causa: " + e.Message + removerPastaNova());
                return false;
            }

        }

        private bool copiarSubPastas()
        {
            try
            {
                var pastas = Directory.GetDirectories(caminhoServico, "*", SearchOption.AllDirectories);
                foreach (var subpasta in pastas)
                {
                    // garanto que a açao de substituir o nome do cliente só vai alterar pastas e arquivos dentro da pasta do serviço e a propria pasta do serviço
                    var novaPasta = caminhoParentServico + subpasta.Replace(caminhoParentServico, "").Replace(nomeCliente, novoNomeCliente);
                    Directory.CreateDirectory(novaPasta);
                    atualizarProgresso(Path.GetFileName(subpasta));

                }
                return true;
            }
            catch (Exception e)
            {
                UiUtils.erroMsg(this.GetType().Name, "Erro copiando pastas: " + e.Message + removerPastaNova());
                return false;
            }
        }

        private bool copiarArquivos()
        {
            try
            {
                var arquivos = Directory.GetFiles(caminhoServico, "*", SearchOption.AllDirectories);
                foreach (var arquivo in arquivos)
                {
                    // garanto que a açao de substituir o nome do cliente só vai alterar pastas e arquivos dentro da pasta do serviço e a propria pasta do serviço
                    var novoArquivo = caminhoParentServico + arquivo.Replace(caminhoParentServico, "").Replace(nomeCliente, novoNomeCliente);
                    File.Copy(arquivo, novoArquivo, true);
                    atualizarProgresso(Path.GetFileName(arquivo));
                }
                return true;
            }
            catch (Exception e)
            {
                UiUtils.erroMsg(this.GetType().Name, "Erro copiando arquivo: " + e.Message + removerPastaNova());
                return false;
            }
        }

        private void atualizarProgresso(string nomeArquivo)
        {
            Async.runOnUI(() =>
            {
                arquivosCopiados++;
                lblStatus.Content = "Copiando " + nomeArquivo + "...";
                pbar.Value = arquivosCopiados * 100 / arquivosAcopiar;
            });
        }

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

        private void removerPastaAntiga()
        {
            try { Directory.Delete(caminhoServico, true); }
            catch (Exception e)
            {
                UiUtils.erroMsg(this.GetType().Name, "Erro removendo pasta de serviço com nome antigo. Remova manualmente. Causa: " + e.Message);
                try
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = Directory.GetParent(caminhoServico)!.FullName,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
                catch (Exception) { UiUtils.erroNot(String.Format("Não foi possível abrir a pasta {0}", caminhoServico)); }
            }
        }

        private void buscarCartaoNoTrello()
        {
            pbar.IsIndeterminate = true;
            lblStatus.Content = "Buscando cartão do serviço...";

            Async.runAsync(() =>
            {
                new TrelloApi().obterCartaoPorNome(Path.GetFileName(caminhoServico), (string? erro, TrelloCard? cartao) =>
                {
                    Async.runOnUI(() =>
                    {
                        if (erro != null)
                        {
                            UiUtils.erroMsg(this.GetType().Name, "Erro buscando pelo cartão do serviço no Trello: " + erro);
                            lblStatus.Content = "...Alterações locais ocorreram com sucesso.";
                            fechar();
                        }
                        else if (cartao == null)
                        {
                            UiUtils.erroMsg(this.GetType().Name, "O cartão de serviço não existe.");
                            lblStatus.Content = "O cartão de serviço não existe.";

                            fechar();
                        }
                        else
                        {
                            this.cartao = cartao;
                            atualizarCartaoNoTrello();
                        }
                    });

                });
            });
        }

        private void atualizarCartaoNoTrello()
        {
            lblStatus.Content = "Renomeando cartão do serviço...";
            Async.runAsync(() =>
            {

                cartao!.Name = Path.GetFileName(novoCaminhoServico!);
                new TrelloApi().renomearCartao(cartao, (String? erro) =>
                {

                    Async.runOnUI(() =>
                    {
                        if (erro != null) UiUtils.erroMsg(this.GetType().Name, "Erro renomeando cartão: " + erro);
                        else
                        {
                            pbar.IsIndeterminate = false;
                            pbar.Value = 100;
                            lblStatus.Content = "Sucesso!";
                        }
                        fechar();
                    });

                });
            });


        }

        private void fechar(int delay = 1000, bool atualizarInterface = true)
        {
            if (atualizarInterface) carregarServicos();
            Async.runOnUI(delay, () => { this.Close(); });
        }

    }

}
