using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace WindowsTerminalQuake
{
    public class Config: ApplicationSettingsBase
    {
        [UserScopedSettingAttribute()]
        [DefaultSettingValue("False")]
        public bool Maximize { get
            {
                return (bool)this["Maximize"];
            }
            set {
                this["Maximize"] = value;
            }
        }
        [UserScopedSettingAttribute()]
        [DefaultSettingValue("True")]
        public bool Center {
            get
            {
                return (bool)this["Center"];
            }

            set
            {
                this["Center"] = value;
            }
        }
        [UserScopedSettingAttribute()]
        [DefaultSettingValue("0")]
        public int Width
        {
            get
            {
                return (int)this["Width"];
            }

            set
            {
                this["Width"] = value;
            }
        }
        [UserScopedSettingAttribute()]
        [DefaultSettingValue("0")]
        public int Height
        {
            get
            {
                return (int)this["Height"];
            }

            set
            {
                this["Height"] = value;
            }
        }
        [UserScopedSettingAttribute()]
        [DefaultSettingValue("0")]
        public int OffsetLeft
        {
            get
            {
                return (int)this["OffsetLeft"];
            }

            set
            {
                this["OffsetLeft"] = value;
            }
        }
    }
}
