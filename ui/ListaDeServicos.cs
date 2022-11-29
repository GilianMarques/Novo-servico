using conta.azul.modelos;
using conta_azul;
using CriadorDePastas.trello;
using FileIO;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using Notification.Wpf;
using NovoServico;
using NovoServico.outros;
using outros;
using Prism.Services.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using trello;
using ui;
using Path = System.IO.Path;
using Timer = System.Timers.Timer;

namespace CriadorDePastas
{
    /// <summary>
    /// Interaction logic for ListaDeServicos.xaml
    /// </summary>
    public partial class ListaDeServicos : Window
    {

        private List<String> servicos = new List<String>();
        private SolidColorBrush corCinza = new SolidColorBrush(Color.FromRgb(250, 250, 250));
        private Timer? timerDePesquisa;
        private readonly TelaPrincipal telaPrincipal;
        private int buscarFeitas = 0;
        private ControleServico utimoControleClicado;

        public bool criarDescricaoeFechar { get; private set; }

        public ListaDeServicos(TelaPrincipal telaPrincipal)
        {
            new WindowStateSaveHelper(this);
            InitializeComponent();
            carregarServicos();
            Async.runOnUI(500, () =>
            {
                tbPesquisa.Focus();
            });

            this.telaPrincipal = telaPrincipal;
        }


        internal void carregarServicos()
        {
            buscarFeitas++;
            pbar.Opacity = 100;
            pbar.Value = 0;

            var chave = tbPesquisa.Text;
            var servicosFeitos = (bool)tbServicosFeitos.IsChecked!;

            Async.runAsync(() =>
            {
                int x = buscarFeitas;
                servicos = new Pastas().lerServicos(chave, servicosFeitos);
                if (x != buscarFeitas) return;
                Async.runOnUI(() =>
                {
                    if (parent.Children.Count > 0) parent.Children.Clear();
                    if (servicos.Count == 0) pbar.Opacity = 0;

                    for (int i = 0; i < servicos.Count; i++)
                    {
                        if (x != buscarFeitas) break;
                        addServicoAlista(servicos[i], i);

                        void addServicoAlista(string caminhoServico, int i)
                        {

                            Async.runOnUI(1, () =>
                            {
                                if (x != buscarFeitas) return;

                                var controle = new ControleServico(telaPrincipal, caminhoServico);
                                controle.definirCallbackDeClique(mudarCorDoUltimoControle);
                                controle.lblBlockNomeServico.Text = Path.GetFileName(caminhoServico);

                                if (i % 2 != 0) controle.parent.Background = corCinza;

                                parent.Children.Add(controle);

                                pbar.Value = i * 100 / servicos.Count;
                                lblInfoDaBusca.Content = i + "/" + servicos.Count;

                                if (i + 1 == servicos.Count)
                                {
                                    lblInfoDaBusca.Content = (i + 1) + "/" + servicos.Count;
                                    pbar.Opacity = 0;
                                    Async.runOnUI(500, () => { lblInfoDaBusca.Content = ""; });
                                }
                            });
                        }
                    }

                });
            });
        }

        private int mudarCorDoUltimoControle(ControleServico controle)
        {
            Console.WriteLine("mudadno cor do controle " + controle.lblBlockNomeServico.Text);

            Brush background = controle.parent.Background;
            if (utimoControleClicado != null)
            {
                utimoControleClicado.parent.Background = background;
            }

            controle.parent.Background = new SolidColorBrush(Color.FromArgb(230, 230, 230, 230));
            utimoControleClicado = controle;
            return 1;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (timerDePesquisa != null)
            {
                timerDePesquisa.Stop();
                timerDePesquisa = null;
            }

            timerDePesquisa = new Timer();
            timerDePesquisa.Interval = 500;
            timerDePesquisa.Elapsed += (object? sender, ElapsedEventArgs e) =>
            {
                timerDePesquisa.Stop();
                Async.runOnUI(() => { carregarServicos(); });
            };
            timerDePesquisa.Start();


        }

        private void tbServicosFeitos_Click(object sender, RoutedEventArgs e)
        {
            carregarServicos();
        }

        private void janelaPerdeuFoco(object sender, EventArgs e)
        {
            tbPesquisa.Focus();
            tbPesquisa.SelectAll();
        }


    }

}
