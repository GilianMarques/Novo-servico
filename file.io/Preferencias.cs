using FileIO;
using Newtonsoft.Json;
using outros;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace file.io
{
    internal class Preferencias
    {

        public static readonly string rootFolder = "_data";
        public static readonly string arquivoDePreferencias = "preferencias.txt";
        public static readonly string caminhoServidor = "serverPath";
        public static readonly string caminhoTemplates = "templatePath";
        public static readonly string trelloUserToken = "trelloUserToken";
        public static readonly string trelloDefLabels = "trelloDefLabels";
        public static readonly string trelloListaIdLayout = "trelloListaIdLayout";
        public static readonly string lastError = "lastError";
        public static readonly string trelloDescTemplate = "trelloDescTemplate";
        public static readonly string trelloTemplateIdContaAzul = "trelloTemplateIdContaAzul";
        public static readonly string contaAzulcred = "contaAzulcred";
        public static readonly string pastaServicosFeitos = "-------  SERVIÇOS FEITOS   -------";
        public static readonly string trelloDesCartaoParcial = "trelloDesCartaoParcial";
        public static readonly string trelloSemOsLabel = "trelloSemOsLabel";
        public static readonly string trelloParcialLabel = "trelloParcialLabel";
        public static readonly string trelloQuadroId = "trelloQuadroId";
        public static readonly string trelloKey = "trelloKey";
        public static readonly string trelloOAtuhSecret = "trelloOAtuhSecret";
        public static readonly string trelloListaEmAndamento = "trelloListaEmAndamento";
        public static readonly string contaAzulLimiteOrdens = "contaAzulLimiteOrdens";
        public static readonly string contaAzulDiasRetroativos = "contaAzulDiasRetroativos";

        /*----------------------------------------------*/

        private static Preferencias? instancia;

        private Dictionary<string, Object> preferencias = new Dictionary<string, object>();
        public string caminhoDasConfiguracoes = "";
        private FileReader reader;
        private FileWriter writer;

        private Preferencias()
        {
            caminhoDasConfiguracoes = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDoc‌​uments), "NovoServico", arquivoDePreferencias);

            if (!Directory.Exists(caminhoDasConfiguracoes)) new FileInfo(caminhoDasConfiguracoes).Directory!.Create();

            reader = new FileReader(caminhoDasConfiguracoes, true);
            writer = new FileWriter(caminhoDasConfiguracoes, true);

            String? dadosNoDisco = reader.readLine(out String? ignore);
            if (dadosNoDisco != null) preferencias = JsonConvert.DeserializeObject<Dictionary<string, Object>>(dadosNoDisco)!;
        }

        public static Preferencias inst()
        {
            if (instancia == null) instancia = new Preferencias();
            return instancia;
        }



        public void save(String chave, object valor)
        {
            if (preferencias.ContainsKey(chave)) preferencias[chave] = valor;
            else preferencias.Add(chave, valor);

            Async.runAsync(() =>
            {
                string? erro = writer.writeToFile(JsonConvert.SerializeObject(preferencias, Formatting.Indented));
                if (erro != null) UiUtils.erroMsg(this.GetType().Name,$"Erro escrevendo configuração no disco. Chave: {chave} Valor: {valor}.\nCausa: {erro} \nA alteração persiste na memoria mas não foi escrita no disco.");
            });
        }
        /// <summary>
        /// Retorna o valor salvo atraves da chave recebida.
        /// Se o valor da chave nao for encontrado e o valor padrao recebido nao for nulo, este será salvo no banco de dados
        /// </summary>
        /// <param name="chave"></param>
        /// <param name="valorPadrao"></param>
        /// <returns></returns>
        public String? getString(String chave, string? valorPadrao = null)
        {
            var prefs = preferencias.GetValueOrDefault(chave);
            if (prefs == null)
            {
                if (valorPadrao != null) save(chave, valorPadrao);
                return valorPadrao;
            }
            else return (String)prefs;
        }

        public int? getInt(String chave, int? valorPadrao = null)
        {
            var prefs = preferencias.GetValueOrDefault(chave);
            if (prefs == null)
            {
                if (valorPadrao != null) save(chave, valorPadrao);
                return valorPadrao;
            }
            else return Int32.Parse(""+prefs);
        }


    }
}
