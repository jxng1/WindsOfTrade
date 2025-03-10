using System.IO;
using System.Xml;

using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Bannerlord.UIExtenderEx.ResourceManager;

namespace WindsOfTrade.Patches
{
    [PrefabExtension("InventoryItemTuple", "//Constants/Constant[1]")]
    internal class InventoryHighlightPrefabBrushExtension : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Prepend;

        [PrefabExtensionFileName]
        public string PatchFileName => "ItemHighlightInsert.xml";

        public InventoryHighlightPrefabBrushExtension()
        {
            if (SubModule.ModuleDirectory is not string basedir)
            {
                return;
            }

            var brushDoc = new XmlDocument();
            brushDoc.Load(Path.Combine(basedir, @"..\..\GUI\Brushes\ItemHighlightBrush.xml"));
            BrushFactoryManager.CreateAndRegister(brushDoc);
        }
    }
}
