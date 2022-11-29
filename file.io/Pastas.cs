using file.io;
using NovoServico.outros;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Interop;
using System.Windows.Media;
using Windows.Storage;

namespace FileIO
{
    internal class Pastas
    {

        public void criarPastaDeServico(string nomeCliente, Action<String?, String> callback)
        {

            var novoNumero = obterProximoNumeroDisponivel();

            string caminho = String.Format("{0}\\SERVIÇO {1} - {2}", lerCaminhoDoServidor(), novoNumero, nomeCliente.ToUpper());

            bool processoFalhou = false;

            try { Directory.CreateDirectory(caminho); }
            catch (Exception ex)
            {
                callback("Erro criando pasta do serviço em: " + caminho + "\n\nDetalhes: " + ex.Message, caminho);
                processoFalhou = true;
            }

            if (!processoFalhou) try { copiarSubpastasDoServico(caminho, novoNumero); }
                catch (Exception ex)
                {
                    callback("Erro copiando PASTAS dos templates."
                    + "\nA pasta de serviço recém criada será removida. Verifique a causa do problema e tente novamente."
                    + "\n\nDetalhes: " + ex.Message + deletarPastaDeServico(), caminho);

                }

            if (!processoFalhou) try { copiarArquivosDoServico(caminho, novoNumero, nomeCliente); }
                catch (Exception ex)
                {
                    callback("Erro copiando ARQUIVOS dos templates."
                   + "\nA pasta de serviço recém criada será removida. Verifique a causa do problema e tente novamente."
                   + "\n\nDetalhes: " + ex.Message + deletarPastaDeServico(), caminho);
                }

            if (!processoFalhou) callback(null, caminho);


            String deletarPastaDeServico()
            {
                processoFalhou = true;
                try
                {
                    Directory.Delete(caminho, true);
                    return "\n\nPasta de serviço recém criada foi removida!";
                }
                catch (Exception ex)
                {
                    return "\n\n--------------- ERRO FATAL! ---------------"
                        + "\n\nNão foi possível remover a pasta de serviço recém criada, caminho:" + caminho
                        + "\nVerifique o que pode ter acontecido antes de tentar novamente."
                        + "\n\nDetalhes: " + ex.Message;
                }
            }


        }

        public int obterProximoNumeroDisponivel()
        {

            int novoNumero = -1;
            string[]? servicos = Directory.GetDirectories(lerCaminhoDoServidor()!);
            string[]? servicosFeitos = Directory.GetDirectories(Path.Combine(lerCaminhoDoServidor()!, Preferencias.pastaServicosFeitos));

            string[] dir = servicos.Concat(servicosFeitos).ToArray();

            foreach (var caminhoPasta in dir)
            {
                //  Debug.WriteLine($":: Prox Num Disponivel: {caminhoPasta} ");
                if (!pastaDeServicosValida(caminhoPasta)) continue;
                int numero = obterNumero(Path.GetFileName(caminhoPasta));
                if (numero >= novoNumero) novoNumero = numero;
            }

            return novoNumero + 1;
        }

        private void copiarSubpastasDoServico(string caminhoDoServico, int novoNumero)
        {

            string path = lerCaminhoPraPastaDeTemplates()!;


            var dir = Directory.GetDirectories(path);

            foreach (var pastaTemplate in dir)
            {
                var nomePasta = Path.GetFileName(pastaTemplate);
                string newFolderPath = String.Format("{0}\\{1} ({2})", caminhoDoServico, nomePasta.ToUpper(), novoNumero);
                Directory.CreateDirectory(newFolderPath);
            }
        }

        private void copiarArquivosDoServico(string caminho, int novoNumero, string nomeCliente)
        {
            string? caminhoTemplates = lerCaminhoPraPastaDeTemplates()!;

            var arquivos = Directory.GetFiles(caminhoTemplates);

            foreach (var caminhoArquivo in arquivos)
            {
                string ext = Path.GetExtension(caminhoArquivo);
                var nomeArquivo = caminhoArquivo.Replace(caminhoTemplates + "\\", "").Replace(ext, "");
                string novoCaminhoArquivo = String.Format("{0}\\{1} - {2} {3}{4}", caminho, nomeArquivo.ToUpper(), nomeCliente, novoNumero, ext.ToLower());

                File.Copy(caminhoArquivo, novoCaminhoArquivo, true);
            }

        }

        public string? lerCaminhoDoServidor()
        {

            String? path = Preferencias.inst().getString(Preferencias.caminhoServidor);

            if (path == null)
            {
                Debug.WriteLine(this.GetType().Name, "Não foi possivel Ler caminho p/ servidor");
                return null;
            }
            else return path;
        }

        public string? lerCaminhoPraPastaDeTemplates()
        {
            String? path = Preferencias.inst().getString(Preferencias.caminhoTemplates);

            if (path == null)
            {
                Debug.WriteLine(this.GetType().Name, "Não foi possivel Ler caminho p/ pasta de templates");
                return null;
            }
            else return path;
        }

        internal bool pastaDeServicosValida(string caminho)
        {
            // exemplo:  T:\SERVIÇO 4130 - ISABEL CRISTINA SARTE
            // esse regex vai coincidir o "\SERVIÇO 4130 - " no caminhoDoServico da pasta
            return new Regex(@"[\\][SERVIÇO]{7}[ ][0-9]+[ ][-][ ]").IsMatch(caminho);

        }

        public int obterNumero(String nomeServiço)
        {   // groups[0]  retorna o match inteiro e nao só o que esta dentro dos parenteses
            return int.Parse(new Regex(@"[SERVIÇO]{7}[ ]([0-9]+)[ ][-][ ]").Match(nomeServiço).Groups[1].ToString());
        }

        public List<String> lerServicos(String? chave, bool servicosFeitos)
        {

            List<String> services = new List<String>();

            String caminhoServidor = servicosFeitos ? $"{Path.Combine(lerCaminhoDoServidor()!, Preferencias.pastaServicosFeitos)}" : lerCaminhoDoServidor()!;
            string[] pastasNoServidor = Directory.GetDirectories(caminhoServidor);

            foreach (string caminhoPasta in pastasNoServidor)
                if (pastaDeServicosValida(caminhoPasta))
                    if (chave != null)
                    {
                        if (Path.GetFileName(caminhoPasta).ToLower().Contains(chave.ToLower())) services.Add(caminhoPasta);
                    }
                    else services.Add(caminhoPasta);

            return services.OrderByDescending(o => obterNumero(o)).ToList().Take(100).ToList();
        }

    }


}
