using Microsoft.Data.Sqlite;
using System.ComponentModel.DataAnnotations;
using vendinha_backend.Models;

namespace vendinha_backend.Services
{
    public class DividaService
    {
        private string ConnectionString = "Data Source=vendinha.db";

        public List<Divida> Consultar(string clienteCpf)
        {
            var conexao = new SqliteConnection(ConnectionString);
            conexao.Open();

            var sql = """
                SELECT id, clientecpf, valor, paga, datacriacao, datapagamento
                FROM dividas
                WHERE clientecpf = @clienteCpf
                ORDER BY datacriacao DESC
            """;

            var comando = new SqliteCommand(sql, conexao);
            comando.Parameters.Add("clienteCpf", SqliteType.Text).Value = clienteCpf;
            var leitor = comando.ExecuteReader();
            var resultado = new List<Divida>();

            while (leitor.Read())
            {
                var divida = new Divida()
                {
                    Id = leitor.GetInt32(0),
                    ClienteCpf = leitor.GetString(1),
                    Valor = Convert.ToDecimal(leitor.GetDouble(2)),
                    Paga = leitor.GetInt32(3) == 1,
                    DataCriacao = DateTime.Parse(leitor.GetString(4)),
                    DataPagamento = leitor.IsDBNull(5) ? null : DateTime.Parse(leitor.GetString(5))
                };
                resultado.Add(divida);
            }
            return resultado;
        }

        public bool Criar(string clienteCpf, Divida divida)
        {
            divida.ClienteCpf = clienteCpf;
            if (!Validar(divida))
            {
                return false;
            }
            if (!ClienteExiste(clienteCpf))
            {
                return false;
            }
            if (PossuiDividaAberta(clienteCpf))
            {
                return false;
            }
            var conexao = new SqliteConnection(ConnectionString);
            conexao.Open();

            var sql = """
                INSERT INTO dividas(clientecpf, valor, paga, datacriacao)
                VALUES (@clienteCpf, @valor, 0, @datacriacao)
            """;

            var comando = new SqliteCommand(sql, conexao);
            comando.Parameters.Add("clienteCpf", SqliteType.Text).Value = clienteCpf;
            comando.Parameters.Add("valor", SqliteType.Real).Value = (double)divida.Valor;
            comando.Parameters.Add("datacriacao", SqliteType.Text).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            var linhas = comando.ExecuteNonQuery();
            return linhas == 1;
        }

        public bool Pagar(int id)
        {
            var conexao = new SqliteConnection(ConnectionString);
            conexao.Open();

            var sql = """
                UPDATE dividas
                SET paga = 1, datapagamento = @datapagamento
                WHERE id = @id AND paga = 0
            """;

            var comando = new SqliteCommand(sql, conexao);
            comando.Parameters.Add("datapagamento", SqliteType.Text).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            comando.Parameters.Add("id", SqliteType.Integer).Value = id;

            var linhas = comando.ExecuteNonQuery();
            return linhas == 1;
        }

        public bool Validar(Divida divida)
        {
            var context = new ValidationContext(divida);
            var erros = new List<ValidationResult>();
            return Validator.TryValidateObject(divida, context, erros, true);
        }

        private bool PossuiDividaAberta(string clienteCpf)
        {
            var conexao = new SqliteConnection(ConnectionString);
            conexao.Open();

            var sql = "SELECT 1 FROM dividas WHERE clientecpf = @clienteCpf AND paga = 0";

            var comando = new SqliteCommand(sql, conexao);
            comando.Parameters.Add("clienteCpf", SqliteType.Text).Value = clienteCpf;

            return comando.ExecuteScalar() != null;
        }

        private bool ClienteExiste(string clienteCpf)
        {
            var conexao = new SqliteConnection(ConnectionString);
            conexao.Open();

            var sql = "SELECT 1 FROM clientes WHERE cpf = @clienteCpf";

            var comando = new SqliteCommand(sql, conexao);
            comando.Parameters.Add("clienteCpf", SqliteType.Text).Value = clienteCpf;

            return comando.ExecuteScalar() != null;
        }
    }
}