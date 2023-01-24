using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace domain
{
    internal class Nome
    {
        public static string aplicarRegras(string nome)
        {
            const string caractereAceito = "";

            var regexBreakline = new Regex(@"(\r\n?|\r?\n)+");
            var regexMultSpaces = new Regex(@"[ ]+");
            var regexCharsProibidosPastasWindows = new Regex(@"[\\\/:?*""<>|]"); // caracteres que nao podem ser usados em nomes de pasta do windows
            var regexCharsProibidosTrello = new Regex(@"[@]"); // caracteres que nao funcionam como nome de cartao no trello, esses caracteres impedem a busca pelo cartao ou causam algum outro tipo de bug


            nome = nome.ToUpper().Trim();
            nome = regexBreakline.Replace(nome, "");
            nome = regexMultSpaces.Replace(nome, " ");
            nome = regexCharsProibidosPastasWindows.Replace(nome, caractereAceito);
            nome = regexCharsProibidosTrello.Replace(nome, caractereAceito);

            return nome;
        }
    }
}
