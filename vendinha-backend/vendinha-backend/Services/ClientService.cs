using Microsoft.Data.Sqlite;
using System.ComponentModel.DataAnnotations;
using vendinha_backend.Models;

namespace vendinha_backend.Services
{
    public class ClienteService
    {
        private string ConnectionString = "Data Source=vendinha.db";

        public List<Cliente> Consultar(int pagina)
        {
            var conexao = new SqliteConnection(ConnectionString);
            conexao.Open();

            var sql = """
                SELECT c.cpf, c.nome, c.email, c.datanascimento,
                       COALESCE(SUM(CASE WHEN d.paga = 0 THEN d.valor END), 0.0) AS totaldividas
                FROM clientes c
                LEFT JOIN dividas d ON d.clientecpf = c.cpf
                GROUP BY c.cpf, c.nome, c.email, c.datanascimento
                ORDER BY totaldividas DESC
                LIMIT 10 OFFSET @paginacao
            """;

            var comando = new SqliteCommand(sql, conexao);
            comando.Parameters.Add("paginacao", SqliteType.Integer).Value = (pagina - 1) * 10;
            var leitor = comando.ExecuteReader();
            var resultado = new List<Cliente>();

            while (leitor.Read())
            {
                var cliente = new Cliente()
                {
                    Cpf = leitor.GetString(0),
                    Nome = leitor.GetString(1),
                    Email = leitor.IsDBNull(2) ? null : leitor.GetString(2),
                    DataNascimento = DateTime.Parse(leitor.GetString(3)),
                    TotalDividas = Convert.ToDecimal(leitor.GetDouble(4))
                };
                resultado.Add(cliente);
            }
            return resultado;
        }

        public List<Cliente> BuscarPorNome(string nome, int pagina)
        {
            var conexao = new SqliteConnection(ConnectionString);
            conexao.Open();

            var sql = """
                SELECT c.cpf, c.nome, c.email, c.datanascimento,
                       COALESCE(SUM(CASE WHEN d.paga = 0 THEN d.valor END), 0.0) AS totaldividas
                FROM clientes c
                LEFT JOIN dividas d ON d.clientecpf = c.cpf
                WHERE c.nome LIKE '%' || @nome || '%'
                GROUP BY c.cpf, c.nome, c.email, c.datanascimento
                ORDER BY totaldividas DESC
                LIMIT 10 OFFSET @paginacao
            """;

            var comando = new SqliteCommand(sql, conexao);
            comando.Parameters.Add("nome", SqliteType.Text).Value = nome;
            comando.Parameters.Add("paginacao", SqliteType.Integer).Value = (pagina - 1) * 10;
            var leitor = comando.ExecuteReader();
            var resultado = new List<Cliente>();

            while (leitor.Read())
            {
                var cliente = new Cliente()
                {
                    Cpf = leitor.GetString(0),
                    Nome = leitor.GetString(1),
                    Email = leitor.IsDBNull(2) ? null : leitor.GetString(2),
                    DataNascimento = DateTime.Parse(leitor.GetString(3)),
                    TotalDividas = Convert.ToDecimal(leitor.GetDouble(4))
                };
                resultado.Add(cliente);
            }
            return resultado;
        }

        public Cliente? BuscarPorCpf(string cpf)
        {
            var conexao = new SqliteConnection(ConnectionString);
            conexao.Open();

            var sql = """
                SELECT c.cpf, c.nome, c.email, c.datanascimento,
                       COALESCE(SUM(CASE WHEN d.paga = 0 THEN d.valor END), 0.0) AS totaldividas
                FROM clientes c
                LEFT JOIN dividas d ON d.clientecpf = c.cpf
                WHERE c.cpf = @cpf
                GROUP BY c.cpf, c.nome, c.email, c.datanascimento
            """;

            var comando = new SqliteCommand(sql, conexao);
            comando.Parameters.Add("cpf", SqliteType.Text).Value = cpf;
            var leitor = comando.ExecuteReader();

            if (leitor.Read())
            {
                return new Cliente()
                {
                    Cpf = leitor.GetString(0),
                    Nome = leitor.GetString(1),
                    Email = leitor.IsDBNull(2) ? null : leitor.GetString(2),
                    DataNascimento = DateTime.Parse(leitor.GetString(3)),
                    TotalDividas = Convert.ToDecimal(leitor.GetDouble(4))
                };
            }
            return null;
        }

        public bool Criar(Cliente cliente)
        {
            if (!Validar(cliente))
            {
                return false;
            }
            if (CpfExiste(cliente.Cpf))
            {
                return false;
            }
            var conexao = new SqliteConnection(ConnectionString);
            conexao.Open();

            var sql = """
                INSERT INTO clientes(cpf, nome, email, datanascimento)
                VALUES (@cpf, @nome, @email, @datanascimento)
            """;

            var comando = new SqliteCommand(sql, conexao);
            comando.Parameters.Add("cpf", SqliteType.Text).Value = cliente.Cpf;
            comando.Parameters.Add("nome", SqliteType.Text).Value = cliente.Nome;
            comando.Parameters.Add("email", SqliteType.Text).Value = (object?)cliente.Email ?? DBNull.Value;
            comando.Parameters.Add("datanascimento", SqliteType.Text).Value = cliente.DataNascimento.ToString("yyyy-MM-dd");

            var linhas = comando.ExecuteNonQuery();
            return linhas == 1;
        }

        public bool Atualizar(string cpf, Cliente cliente)
        {
            cliente.Cpf = cpf;
            if (!Validar(cliente))
            {
                return false;
            }
            var conexao = new SqliteConnection(ConnectionString);
            conexao.Open();

            var sql = """
                UPDATE clientes
                SET nome = @nome, email = @email, datanascimento = @datanascimento
                WHERE cpf = @cpf
            """;

            var comando = new SqliteCommand(sql, conexao);
            comando.Parameters.Add("cpf", SqliteType.Text).Value = cliente.Cpf;
            comando.Parameters.Add("nome", SqliteType.Text).Value = cliente.Nome;
            comando.Parameters.Add("email", SqliteType.Text).Value = (object?)cliente.Email ?? DBNull.Value;
            comando.Parameters.Add("datanascimento", SqliteType.Text).Value = cliente.DataNascimento.ToString("yyyy-MM-dd");

            var linhas = comando.ExecuteNonQuery();
            return linhas == 1;
        }

        public bool Validar(Cliente cliente)
        {
            var context = new ValidationContext(cliente);
            var erros = new List<ValidationResult>();
            return Validator.TryValidateObject(cliente, context, erros, true);
        }

        public bool Excluir(string cpf)
        {
            var conexao = new SqliteConnection(ConnectionString);
            conexao.Open();
            var transacao = conexao.BeginTransaction();

            try
            {
                var sqlDividas = "DELETE FROM dividas WHERE clientecpf = @cpf";
                var comandoDividas = new SqliteCommand(sqlDividas, conexao, transacao);
                comandoDividas.Parameters.Add("cpf", SqliteType.Text).Value = cpf;
                comandoDividas.ExecuteNonQuery();

                var sql = "DELETE FROM clientes WHERE cpf = @cpf";
                var comando = new SqliteCommand(sql, conexao, transacao);
                comando.Parameters.Add("cpf", SqliteType.Text).Value = cpf;

                var linhas = comando.ExecuteNonQuery();
                if (linhas == 1)
                {
                    transacao.Commit();
                    return true;
                }
                transacao.Rollback();
                return false;
            }
            catch
            {
                transacao.Rollback();
                throw;
            }
        }

        private bool CpfExiste(string cpf)
        {
            var conexao = new SqliteConnection(ConnectionString);
            conexao.Open();

            var sql = "SELECT 1 FROM clientes WHERE cpf = @cpf";

            var comando = new SqliteCommand(sql, conexao);
            comando.Parameters.Add("cpf", SqliteType.Text).Value = cpf;

            return comando.ExecuteScalar() != null;
        }
    }
}