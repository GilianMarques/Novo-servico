using conta.azul.modelos;
using file.io;

using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NovoServico;
using NovoServico.outros;
using outros;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Text.Json.Nodes;
using System.Windows.Documents;
using trello;
using trello.modelos;
using ui;
using unirest_net.http;
using Windows.Media.Protection.PlayReady;
using Windows.Storage.Streams;
using Windows.Web.Http;

namespace CriadorDePastas.trello
{
    // ajuda: https://code-maze.com/different-ways-consume-restful-api-csharp/
    public class TrelloApi
    {
        private static String KEY = Preferencias.inst().getString(Preferencias.trelloKey, "Seus_dados_aqui")!;
        private static String OAUTH_SECRET = Preferencias.inst().getString(Preferencias.trelloOAtuhSecret, "Seus_dados_aqui")!;

        private static string semOsLabel = Preferencias.inst().getString(Preferencias.trelloSemOsLabel, "Seus_dados_aqui")!;
        private static string parcialLabel = Preferencias.inst().getString(Preferencias.trelloParcialLabel, "Seus_dados_aqui")!;
        private static string quadroId = Preferencias.inst().getString(Preferencias.trelloQuadroId, "Seus_dados_aqui")!;
        public static string listaEmAndamento = Preferencias.inst().getString(Preferencias.trelloListaEmAndamento, "Seus_dados_aqui")!;

        string? userToken;

        public TrelloApi()
        {
            userToken = Preferencias.inst().getString(Preferencias.trelloUserToken);
        }

        public string verificarConexao()
        {

            try
            {
                HttpResponse<String> response = Unirest.get("https://api.trello.com/1/search?"
               + "modelTypes=cards"
               + "&cards_limit=1"
               + "&query=name:SERVIÇO"
               + "&partial=true"
               + "&key=" + KEY
               + "&token=" + userToken)
             .header("Accept", "application/json").asString();

                Debug.WriteLine($":: verificarConexao {response.Body}");

                return (response.Code == 200 ? "Status Trello: Autenticado" : "Status Trello: Erro: " + response.Body);
            }
            catch (Exception e)
            {
                return  "Status Trello: Erro: " + e.Message;
            }

        }

        public void criarCartao(string nomePasta, Action<String?, String?> callback)
        {
            var now = DateTime.Now;

            HttpResponse<String> response = Unirest.post("https://api.trello.com/1/cards")
              .header("Accept", "application/json")
              .field("idList", Preferencias.inst().getString(Preferencias.trelloListaIdLayout))
              .field("key", KEY)
              .field("token", Preferencias.inst().getString(Preferencias.trelloUserToken))
              .field("name", nomePasta)
              .field("desc", "Não há descrição/ordem de venda para este serviço")
              .field("pos", "top")
              .field("idLabels", semOsLabel + "," + Preferencias.inst().getString(Preferencias.trelloDefLabels))
              .field("start", now.Year + "//" + now.Month + "//" + now.Day + " 21:00:00")
              .field("due", now.Year + "//" + now.Month + "//" + now.Day + " 21:00:00")
              .asString();


            if (response.Code != 200) callback("Erro criando cartão no trello. Abaixo detalhes do erro.\n\n" + response.Code + "\n---------\n" + response.Body, null);
            else
            {
                dynamic cartao = JObject.Parse(response.Body);
                callback(null, "" + cartao.url);
            }
        }

        public void criarCartaoComOs(string nomePasta, OrdemDeVenda ordemDeVenda, String descricao,Action<String?, String?> callback)
        {
            var now = DateTime.Now;

            HttpResponse<String> response = Unirest.post("https://api.trello.com/1/cards")
              .header("Accept", "application/json")
              .field("idList", Preferencias.inst().getString(Preferencias.trelloListaIdLayout))
              .field("key", KEY)
              .field("token", userToken)
              .field("name", nomePasta)
              .field("desc", descricao)
              .field("pos", "top")
              .field("idLabels", Preferencias.inst().getString(Preferencias.trelloDefLabels))
              .field("start", now.Year + "//" + now.Month + "//" + now.Day + " 21:00:00")
              .field("due", ordemDeVenda.dataDeEntrega.Year + "//" + ordemDeVenda.dataDeEntrega.Month + "//" + ordemDeVenda.dataDeEntrega.Day + " 21:00:00")
              .asString();


            if (response.Code != 200) callback("Erro criando cartão no trello. Abaixo detalhes do erro.\n\n" + response.Code + "\n---------\n" + response.Body, null);
            else
            {
                dynamic cartao = JObject.Parse(response.Body);
                callback(null, "" + cartao.url);
            }

        }

        internal void obterCartaoPorNome(string nomeServico, Action<string?, TrelloCard?> callback)
        {
            HttpResponse<String> response = Unirest.get("https://api.trello.com/1/search?"
                           + "modelTypes=cards"
                           + "&query=name:" + nomeServico
                           + "&key=" + KEY
                           + "&idBoards=" + quadroId // BUSCA APENAS NESSE QUADRO
                           + "&token=" + userToken)
                         .header("Accept", "application/json").asString();
            if (response.Code == 200)
            {
                Debug.WriteLine(":: " + response.Body);
                Debug.WriteLine(":: " + nomeServico);
                var cartoes = JsonConvert.DeserializeObject<Cartoes>(response.Body);
                if (cartoes!.Cards.Count > 1) retornarCartaoCorreto(cartoes, nomeServico, callback);
                else if (cartoes.Cards.Count == 0) callback(null, null);
                else callback(null, cartoes.Cards[0]);
            }
            else callback(response.Code + ": " + response.Body, null);

        }

        private void retornarCartaoCorreto(Cartoes cartoes, string nomeServico, Action<string?, TrelloCard?> callback)
        {
            TrelloCard? alvo = null;

            foreach (var cartao in cartoes.Cards) if (cartao.Name.Equals(nomeServico))
                {
                    alvo = cartao;
                    break;
                }

            if (alvo != null) callback(null, alvo);
            else callback($"Nenhum dos cartões encontrados tinha o nome igual a ${nomeServico}.", null);
        }

        internal void anexarOsAoCartao(TrelloCard cartao, Action<string?> callback)
        {


            HttpResponse<String> response = Unirest.put("https://api.trello.com/1/cards/" + cartao.Id
              + "?key=" + KEY
              + "&token=" + Preferencias.inst().getString(Preferencias.trelloUserToken)
              + "&desc=" + Uri.EscapeDataString(cartao.Desc)
              + "&due=" + cartao.Due!.Value.Year + "//" + cartao.Due!.Value.Month + "//" + cartao.Due!.Value.Day + " 21:00:00")
          .header("Accept", "application/json").asString();

            Debug.WriteLine("nome " + cartao.Name + " ->" + response.Code + ": " + response.Body);
            if (response.Code == 200)
            {
                HttpResponse<String> response2 = Unirest.delete("https://api.trello.com/1/cards/" + cartao.Id + "/idLabels/" + semOsLabel
                             + "?key=" + KEY
                             + "&token=" + Preferencias.inst().getString(Preferencias.trelloUserToken))
                          .header("Accept", "application/json").asString();

                if (response2.Code == 200) callback(null);
                else callback("O cartão foi atualizado porém a etiqueta 'SEM OS' não pode ser removida, remova manualmente. Causa:" + response2.Code + ":" + response2.Body);

            }
            else callback(response.Code + ": " + response.Body);


        }

        internal void renomearCartao(TrelloCard cartao, Action<string?> callback)
        {


            HttpResponse<String> response = Unirest.put("https://api.trello.com/1/cards/" + cartao.Id
                + "?key=" + KEY
                + "&token=" + Preferencias.inst().getString(Preferencias.trelloUserToken)
                + "&name=" + cartao.Name)
            .header("Accept", "application/json")
            .asString();
            Debug.WriteLine("nome " + cartao.Name + " ->" + response.Code + ": " + response.Body);
            if (response.Code == 200) callback(null); else callback(response.Code + ": " + response.Body);

        }

        internal Resultado<TrelloCard> criarCartaoParcial(TrelloCard cartaoPrincipal, string nomeCartaoParcial)
        {

            HttpResponse<String> response = Unirest.post("https://api.trello.com/1/cards")
              .header("Accept", "application/json")
              .field("idList", Preferencias.inst().getString(Preferencias.trelloListaIdLayout))
              .field("key", KEY)
              .field("token", userToken)
              .field("name", nomeCartaoParcial)
              .field("desc", Preferencias.inst().getString(Preferencias.trelloDesCartaoParcial, "###Veja detalhes desse item no  cartão anexado.\n\n-----------------\n\n>**Ao fim do serviço, esse cartão deverá ser excluido restando apenas o cartão principal do serviço para ser arquivado.**\n"))
              .field("pos", "top")
              .field("idLabels", parcialLabel)
              .asString();

            Resultado<TrelloCard> res;


            if (response.Code != 200) res = new Resultado<TrelloCard>(null, $"Erro criando cartão parcial no trello. Abaixo detalhes do erro.\n\n {response.Code}: {response.Body}");
            else
            {
                Debug.WriteLine(":: reposta cartao parcial: " + response.Body);
                var card = JsonConvert.DeserializeObject<TrelloCard>(response.Body);
                res = new Resultado<TrelloCard>(card, null);
            }
            return res;


        }

        internal void baixarAnexos(string cartaoId, Action<string?, List<Anexo>> callback)
        {
            HttpResponse<String> response = Unirest.get($"https://api.trello.com/1/cards/{cartaoId}/attachments?"
                          + $"key={KEY}"
                          + $"&token={userToken}")
                        .header("Accept", "application/json").asString();

            Debug.WriteLine($":: anexos {response.Body}");

            if (response.Code == 200)
            {
                try
                {
                    //TALVEZ NAO TENHA ANEXOS NO CARTAO, AI O JSONCONVERT JOGA UMA EXCEÇAO
                    var anexos = JsonConvert.DeserializeObject<List<Anexo>>(response.Body);
                    callback(null, anexos == null ? new List<Anexo>() : anexos);
                }
                catch (Exception)
                {
                    //callback(null, new List<Anexo>());
                    throw;


                }
            }
            else callback($"Erro baixando anexos {response.Code}: {response.Body}", null);
        }

        internal Resultado<String> anexarCapaEmCartaoParcial(TrelloCard cartao, Uri capaUrl)
        {
            var resultado = new ImageDownloader().download(KEY, userToken, capaUrl);

            byte[]? bytes;

            if (resultado.erro != null) return new Resultado<string>(null, $"Erro baixando capa: {resultado.erro}");
            else bytes = File.ReadAllBytes(resultado.valor!);

            HttpResponse<String> response = Unirest.post($"https://api.trello.com/1/cards/{cartao.Id}/attachments?")
                      .header("Accept", "application/json")
                      .field("key", KEY)
                      .field("token", userToken)
                      .field("file", bytes)
                      .field("name", cartao.Name.Split(" - ")[1]) //  1234 - ITEM 2
                      .asString();


            if (response.Code != 200) return new Resultado<string>(null, $"Erro anexando capa: {response.Code}: {response.Body}");
            else return new Resultado<string>(null, null); //sucesso

        }

        internal Resultado<TrelloCard> anexarUrlAoCartao(Uri urlDoAnexo, string idCartao)
        {
            HttpResponse<String> response = Unirest.post($"https://api.trello.com/1/cards/{idCartao}/attachments?")
           .header("Accept", "application/json")
           .field("key", KEY)
           .field("token", userToken)
           .field("url", urlDoAnexo)
           .field("name", urlDoAnexo)
           .asString();


            if (response.Code == 200) return new Resultado<TrelloCard>(null, null);
            else return new Resultado<TrelloCard>(null, $"Erro anexando url.\n\n {response.Code}: {response.Body}");
        }

        internal Resultado<TrelloCard> moverCartaoParaLista(string cartaoId, string listaId)
        {
            HttpResponse<String> response = Unirest.put("https://api.trello.com/1/cards/" + cartaoId
          + "?key=" + KEY
          + "&token=" + userToken
          + "&pos=top"
          + "&idList=" + listaId)
         .header("Accept", "application/json").asString();

            if (response.Code == 200) return new Resultado<TrelloCard>(null, null);
            else return new Resultado<TrelloCard>(null, $"Erro movendo cartão: {response.Code}: {response.Body}");

        }
    }
}
