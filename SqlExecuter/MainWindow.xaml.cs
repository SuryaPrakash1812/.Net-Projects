using Microsoft.Data.SqlClient;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

namespace SqlExecuter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
            private string _selectedConnectionString = "";

            public MainWindow()
            {
                InitializeComponent();
                LoadEnvironments();
            }

            private void LoadEnvironments()
            {
                var envs = ConfigurationManager.ConnectionStrings
                            .Cast<ConnectionStringSettings>()
                            .Select(c => c.Name)
                            .ToList();

                EnvCombo.ItemsSource = envs;
            }

            private void EnvCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
            {
                if (EnvCombo.SelectedItem == null) return;

                var env = EnvCombo.SelectedItem.ToString();
                _selectedConnectionString = ConfigurationManager.ConnectionStrings[env].ConnectionString;
            }

            private async void ExecuteSql_Click(object sender, RoutedEventArgs e)
            {
                if (string.IsNullOrEmpty(_selectedConnectionString))
                {
                    MessageBox.Show("Select environment first!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(SqlEditor.Text))
                {
                    MessageBox.Show("SQL cannot be empty!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string env = EnvCombo.SelectedItem?.ToString() ?? "";
                string sql = SqlEditor.Text.Trim();

                // ⚠ Confirmation message
                var result = MessageBox.Show(
                    $"You are about to execute SQL on the environment: {env}\n\n" +
                    $"Are you sure?\n\nSQL:\n{sql.Substring(0, Math.Min(300, sql.Length))}...",
                    "Confirm Execution",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                try
                {
                    DataTable dt = await ExecuteSqlAsync(sql);
                    ResultGrid.ItemsSource = dt.DefaultView;
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SqlLogs");
                Directory.CreateDirectory(logDir); // ensures folder exists

                string logFile = Path.Combine(logDir, "executed_sql.log");

                string logEntry =
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Environment: {env}{Environment.NewLine}" +
                    $"SQL:{Environment.NewLine}{sql}{Environment.NewLine}" +
                    $"------------------------------------------------------------{Environment.NewLine}";

                File.AppendAllText(logFile, logEntry);

                MessageBox.Show("Execution completed.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error:\n{ex.Message}", "Execution Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            private async Task<DataTable> ExecuteSqlAsync(string sql)
            {
                return await Task.Run(() =>
                {
                    using var con = new SqlConnection(_selectedConnectionString);
                    using var cmd = new SqlCommand(sql, con);
                    using var da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();

                    con.Open();
                    da.Fill(dt);

                    return dt;
                });
            }
        }
        
    }