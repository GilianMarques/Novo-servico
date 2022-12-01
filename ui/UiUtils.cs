using Notification.Wpf;
using outros;
using System;
using System.Diagnostics;
using System.Media;
using System.Windows;

internal class UiUtils
{


    internal static void erroMsg(string classe, string msg)
    {
        Debug.WriteLine($"erro: {classe}: {msg}");
        MessageBox.Show(msg, $"{classe} Erro", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    internal static void erroNot(String msg)
    {
        Async.runOnUI(() =>
        {
            Debug.WriteLine(msg);

            var notificationManager = new NotificationManager();
            notificationManager.Show("Erro", msg, NotificationType.Error);
            SystemSounds.Hand.Play();
        });

    }
    internal static void notificarSemSom(String msg)
    {
        Async.runOnUI(() =>
        {
            Debug.WriteLine(msg);

            var notificationManager = new NotificationManager();
            notificationManager.Show("Novo Serviço", msg, NotificationType.None);
       });

    }

    internal static void sucessoMsg(string msg)
    {
        Debug.WriteLine(msg);
        MessageBox.Show(msg, "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);

    }
}