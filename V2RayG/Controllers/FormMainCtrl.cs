namespace V2RayG.Controllers
{
    class FormMainCtrl : BaseClasses.FormController
    {

        public void Cleanup()
        {
            foreach (var component in GetAllComponents())
            {
                var c = component.Value as Controllers.FormMainComponent.FormMainComponentController;
                c.Cleanup();
            }
        }
    }
}
