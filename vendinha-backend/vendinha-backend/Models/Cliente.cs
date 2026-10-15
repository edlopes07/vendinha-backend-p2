using System;
using System.ComponentModel.DataAnnotations;

namespace vendinha_backend.Models
{
    public class Cliente
    {
        private string cpf;

        [Required(ErrorMessage = "CPF é obrigatório."), StringLength(11, MinimumLength = 11, ErrorMessage = "CPF deve ter 11 números.")]
        public string Cpf 
        { 
            get
            {
                return cpf;
            }
            set 
            { 
                cpf = value == null ? null : new string(value.Where(char.IsDigit).ToArray());
            }
        }

        [Required(ErrorMessage = "Nome é obrigatório."), StringLength(80)]
        public string Nome { get; set; }

        private string email;

        [EmailAddress]
        public string Email 
        { 
            get
            {  
                return this.email; 
            }
            set 
            {
                this.email = value?.ToLower().Trim(); 
            }
        }

        [Required(ErrorMessage = "Data de Nascimento é obrigatória.")]
        public DateTime DataNascimento { get; set; }

        [Range(15, 100)]
        public int Idade
        {
            get
            {
                var hoje = DateTime.Today;
                var idade = hoje.Year - DataNascimento.Year;
                var hojeAnoNascimento = hoje.AddYears(-idade);
                if (DataNascimento > hojeAnoNascimento)
                {
                    idade--;
                }
                return idade;
            }
        }

        public decimal TotalDividas { get; set; }
    }   
}
