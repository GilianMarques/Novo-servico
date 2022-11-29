using System;
using System.Collections.Generic;
using System.Text;

namespace outros
{
    class Resultado<T>
    {
        public T? valor { get; set; }
        
        public String? erro = null;
  

        public Resultado(T? valor, string? erro)
        {
            this.valor = valor;
            this.erro = erro;
        }

        public Resultado()
        {
        }
    }
}
