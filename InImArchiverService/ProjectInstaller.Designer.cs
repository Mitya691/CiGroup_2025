namespace InImArchiverService
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором компонентов

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.InImArchiverServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.InImArchiverServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // InImArchiverServiceProcessInstaller
            // 
            this.InImArchiverServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.InImArchiverServiceProcessInstaller.Password = null;
            this.InImArchiverServiceProcessInstaller.Username = null;
            // 
            // InImArchiverServiceInstaller
            // 
            this.InImArchiverServiceInstaller.Description = "Архивация переменных контроллера S7.";
            this.InImArchiverServiceInstaller.ServiceName = "InIm: Archiving Service";
            this.InImArchiverServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.InImArchiverServiceProcessInstaller,
            this.InImArchiverServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller InImArchiverServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller InImArchiverServiceInstaller;
    }
}