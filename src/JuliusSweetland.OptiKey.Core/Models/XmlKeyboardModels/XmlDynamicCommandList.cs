using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace JuliusSweetland.OptiKey.Models
{
    class XmlDynamicCommandList
    {
        [XmlElement("Action", typeof(DynamicAction))]
        [XmlElement("ChangeKeyboard", typeof(DynamicLink))]
        [XmlElement("KeyDown", typeof(DynamicKeyDown))]
        [XmlElement("KeyUp", typeof(DynamicKeyUp))]
        [XmlElement("KeyToggle", typeof(DynamicKeyToggle))]
        [XmlElement("KeyPress", typeof(DynamicKeyPress))]
        [XmlElement("Loop", typeof(DynamicLoop))]
        [XmlElement("Plugin", typeof(DynamicPlugin))]
        [XmlElement("MoveWindow", typeof(DynamicMove))]
        [XmlElement("Text", typeof(DynamicText))]
        [XmlElement("Wait", typeof(DynamicWait))]
        public List<XmlDynamicKey> Commands { get; } = new List<XmlDynamicKey>();
    }
}
