using Abr.Civil.Sdk;

namespace AbrCivil.Modules
{
    internal class AboutDialog : AbrAboutForm
    {
        public AboutDialog() : base(
            "Библиотека модулей",
            "Каталог модулей ABR | CIVIL для Autodesk Civil 3D.\r\n\r\n" +
            "Устанавливает, обновляет и удаляет модули без прав администратора: " +
            "файлы кладутся в профиль пользователя, в папку ApplicationPlugins.\r\n\r\n" +
            "Установка, обновление и удаление вступают в силу после перезапуска Civil 3D.")
        {
        }
    }
}
