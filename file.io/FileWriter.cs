using file.io;
using NovoServico.outros;
using System;
using System.IO;

namespace FileIO
{
    internal class FileWriter
    {
        private String path;



        /// <summary>
        /// le o arquivo no destino do caminho recebido
        /// </summary>
        /// <param name="path"> o caminho do arquivo de destino</param>
        public FileWriter(string path, bool externo = false)
        {
            if (externo) this.path = path; // define que as operaçoes serao feitas fora da pasta raiz do app
            else this.path = Path.Combine(Preferencias.rootFolder, path);
        }


        /// <summary>
        ///     escreve a informaçao no arquivo, substituindo a antiga, se o arquivo nao existir o cria
        /// </summary>
        /// <param name="error"> para relatar ao usuario qqer erro que posa acontecer durante o processo</param>
        public string? writeToFile(string data)
        {

            try
            {
                File.WriteAllText(path, data);
                return null;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }


    }
}
