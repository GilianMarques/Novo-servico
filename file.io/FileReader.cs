using file.io;
using NovoServico.outros;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileIO
{
    internal class FileReader
    {

        private String path;


        /// <summary>
        /// le o arquivo no destino do caminho
        /// </summary>
        /// <param name="path">nomeServico do arquivo a ser lido</param>
        public FileReader(string path, bool externo = false)
        {
            // se tiver dando erro ao ler os arquivos, veja se a pasta _data existe e crie se n existir.

            if (externo) this.path = path; // define que as operaçoes serao feitas fora da pasta raiz do app
            else this.path = Path.Combine(Preferencias.rootFolder, path);
        }

        /// <summary>
        /// retorna o conteudo do arquivo em uma unica string
        /// </summary>
        /// <param name="errorType"> para relatar ao usuario qqer erro que possa acontecer durante o processo</param>
        /// <returns>uma string com os dados ou null</returns>
        public String? readLine(out String? errorType)
        {
            try
            {
                errorType = null;
                return File.ReadAllText(path);
            }
            catch (Exception e)
            {
                errorType = e.Message;
                return null;
            }
        }


        /// <summary>
        /// retorna cada linha do arquivo em uma posição do array
        /// </summary>
        /// <param name="errorType"> para relatar ao usuario qqer erro que possa acontecer durante o processo</param>
        /// <returns> o array com as informçoes ou null</returns>
        public string[]? readLines(out String? errorType)
        {
            try
            {
                errorType = null;
                return System.IO.File.ReadAllLines(path);
            }
            catch (Exception e)
            {
                errorType = e.Message;
                return null;
            }
        }
    }
}
