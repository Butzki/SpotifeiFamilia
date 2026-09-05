using SpotifeiFamilia.Model;
using SpotifeiFamilia.Views;

namespace SpotifeiFamilia.Controllers;

public class MenuController
{
    private readonly MenuView view = new();

    public void ExibirMenuInicial()
    {
        bool executando = true;

        while (executando)
        {
            string opcao = view.ExibirMenuInicial();

            switch (opcao)
            {
                case "1":
                    new AutenticacaoController().RegistrarUsuario();
                    break;

                case "2":
                    var usuario = new AutenticacaoController().Login();
                    if (usuario != null)
                        MenuPrincipal(usuario);
                    break;

                case "3":
                    executando = false;
                    view.Saindo();
                    break;

                default:
                    view.OpcaoInvalida();
                    break;
            }
        }
    }

    private void MenuPrincipal(Usuario usuario)
    {
        bool ativo = true;

        while (ativo)
        {
            string opcao = view.ExibirMenuPrincipal();

            switch (opcao)
            {
                case "1": new MusicaController(usuario.Id).VerMusicas(); break;
                case "2": new MusicaController(usuario.Id).BuscarMusica(); break;
                case "3": new MusicaController(usuario.Id).ReproduzirMusica(); break;
                case "4": new PlaylistController(usuario.Id).MenuPlaylists(); break;
                case "5": new FamiliaController(usuario.Id).Executar(); break;
                case "6": ativo = false; break;
                default: view.OpcaoInvalida(); break;
            }
        }
    }
}
