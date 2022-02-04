using V2RayG.BaseClasses;

namespace V2RayG.Controllers.FormMainComponent
{
    abstract class FormMainComponentController : IFormComponentController
    {
        private FormComponentController auxComponentController
            = new FormComponentController();

        #region public method
        public void Bind(BaseClasses.FormController container)
        {
            auxComponentController.Bind(container);
        }
        #endregion

        #region abstract method
        public abstract void Cleanup();

        #endregion

        #region protected method
        protected FormMainCtrl GetContainer()
        {
            return auxComponentController.GetContainer<FormMainCtrl>();
        }
        #endregion

        #region private method
        #endregion
    }
}
