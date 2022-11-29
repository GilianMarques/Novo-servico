using file.io;
using NovoServico;
using NovoServico.outros;
using System;
using System.Diagnostics;
using System.IO;

/// <summary>
/// Summary description for Class1
/// </summary>
public class ProgramSp
{
    [STAThread]
    static void Main(string[] args)
    {
        if (args != null && args.Length > 0)
            Console.WriteLine(args[0]);

        var application = new App();
        application.InitializeComponent();
        application.Run();

        // verifica se existe a pasta de dados do app e cria se n existir
        Directory.CreateDirectory(Preferencias.rootFolder);
        //tbm verifica e cria  as pastas no caminho para as preferencias alem de inicializar o singleton
        if (!Directory.Exists(Preferencias.inst().caminhoDasConfiguracoes)) new FileInfo(Preferencias.inst().caminhoDasConfiguracoes).Directory!.Create();
    }
}

