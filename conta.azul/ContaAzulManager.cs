using conta.azul;
using conta.azul.modelos;
using conta_azul;
using file.io;
using FileIO;
using MaterialDesignColors;
using NovoServico.outros;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows.Input;
using unirest_net.http;

/// <summary>
/// Summary description for Class1
/// </summary>
public class ContaAzulManager
{
    private static ContaAzulManager? _instance;

    public static ContaAzulManager GetInstance()
    {
        if (_instance == null)
        {
            _instance = new ContaAzulManager();
        }
        return _instance;
    }

    public void verificarConexao(Action<String?> callback)
    {
        obterCredenciais((ContaAzulCredModel? cred, String? erro) =>
        {
            try
            {
                if (erro != null) { callback(erro); return; }
                Debug.WriteLine(":: obtive credenciais");

                HttpResponse<String> response = Unirest.get("https://api.contaazul.com/v1/sales?size=1")
                            .header("Accept", "application/json")
                            .header("Authorization", "Bearer " + cred!.AccessToken)
                           .asString();

                if (response.Code != 200) callback("Erro baixando dados do Conta Azul " + response.Code + ": " + response.Body);
                else callback("Autenticado");
            }
            catch (Exception e) { callback($"Erro baixando dados do Conta Azul {e.Message}"); }

        });
    }


    public void obterCredenciais(Action<ContaAzulCredModel?, String?> mCallback)
    {
        Debug.WriteLine(":: buscando credenciais conta azul");

        ContaAzulAuth auth = new ContaAzulAuth();
        String? file = Preferencias.inst().getString(Preferencias.contaAzulcred);

        if (file == null) mCallback(null, "Credenciais não encontradas no disco.");
        else
        {
            var credenciais = ContaAzulCredModel.FromJson(file);

            if (credenciais.expirou()) auth.atualizarTokenDeAcesso(credenciais, (String? erro) =>
            {
                String? file = Preferencias.inst().getString(Preferencias.contaAzulcred);
                mCallback(ContaAzulCredModel.FromJson(file), erro);
            });
            else mCallback(credenciais, null);

        }

    }

    internal void carregarVenda(string numOS, Action<string?, OrdemDeVenda?> callback)
    {

        obterCredenciais((ContaAzulCredModel? credenciais, String? erro) =>
        {
            if (erro != null) callback(erro, null);
            else carregarVenda(numOS, credenciais!, callback);

        });
    }

    private void carregarVenda(string numOs, ContaAzulCredModel credenciais, Action<String?, OrdemDeVenda?> callback)
    {
        // vai baixar no maximo esse n° de ordens do Conta Azul
        int limiteDeOrdens = (int)Preferencias.inst().getInt(Preferencias.contaAzulLimiteOrdens, 500)!;
        // buscar por OS de ate x dias atras
        DateTime dataLimite = DateTime.Now.AddDays((double)(-1 * Preferencias.inst().getInt(Preferencias.contaAzulDiasRetroativos, 90)!));

        OrdemDeVenda[]? vendas;


        vendas = baixarOrdensDoContaAzul(limiteDeOrdens, dataLimite, credenciais, out String? erro);

        if (erro != null) callback(erro, null);
        else if (vendas == null) callback("Array de ordens retornado é nulo\n(ContaAzulMAnager.carregarVenda)", null);
        else
        {
            Debug.WriteLine($":: ordens encontradas: {vendas.Length}");
            
            OrdemDeVenda? vendaAlvo = null;

            for (int i = 0; i < vendas.Length; i++)
            {
              //  Debug.WriteLine(":: " + i + " " + vendas[i].Number + " " + vendas[i].Customer.Name + " " + vendas[i].Emission.ToString());

                if (vendas[i].Number.ToString() == numOs)
                {
                    vendaAlvo = vendas[i];
                    break;
                }
            }

            if (vendaAlvo != null) carregarItensDaVenda(vendaAlvo, credenciais, callback);
            else callback("Nao foi encontrada OS com o número '" + numOs + "' emitida a partir de " + dataLimite.ToString()
                + "\n\nLimite de ordens: " + limiteDeOrdens
                + "\n\nOrdens encontradas: " + vendas?.Length, null);

        }





    }

    private OrdemDeVenda[]? baixarOrdensDoContaAzul(int limiteDeOrdens, DateTime dataLimite, ContaAzulCredModel credenciais, out string? erro)
    {
        var emissionStat = dataLimite.ToString();
        var emissionEnd = DateTime.Now.ToString();
        erro = null;



        HttpResponse<String> response = Unirest.get("https://api.contaazul.com/v1/sales?emission_start=" + emissionStat + "&emission_end=" + emissionEnd + "&size=" + limiteDeOrdens)
                    .header("Accept", "application/json")
                    .header("Authorization", "Bearer " + credenciais.AccessToken)
                   .asString();


        if (response.Code != 200)
        {
            erro = "Erro baixando dados do Conta Azul\n\n" + response.Code + ": " + response.Body;
            return null;
        }
        else return OrdemDeVenda.FromJson(response.Body.ToString()).ToArray();

    }

    private void carregarItensDaVenda(OrdemDeVenda venda, ContaAzulCredModel credenciais, Action<String?, OrdemDeVenda> callback)
    {


        HttpResponse<String> response = Unirest.get("https://api.contaazul.com/v1/sales/" + venda.Id + "/items")
                           .header("Accept", "application/json")
                           .header("Authorization", "Bearer " + credenciais.AccessToken)
                           .asString();


        if (response.Code != 200) callback("Erro baixando itens do pedido de venda '" + venda.Number + "''\n\n" + response.Code + "\n" + response.Body + "\n", null);

        else
        {
            venda.ListaDeItens = ListaDeItens.FromJson(response.Body.ToString()).ToArray();
            callback(null, venda);
        }
    }

}
