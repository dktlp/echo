using System;

namespace Echo.Network
{
    public enum SocketControlCharacters : byte
    {
        /// <summary>
        /// Start of heading; Indicates the beginning of a heading in a transmission. The heading can be terminated by STX. As per ASCII-1968, a heading constitutes a machine-sensible address or routing information. Later standards have dropped the explanation.
        /// </summary>
        SOH = 0x01,

        /// <summary>
        /// Start of text; STX has two functions in a transmission: it 1) indicates the beginning of a text and 2) may terminate a heading (see SOH). As per ASCII-1968, text is what should be transmitted to a destination. Later standards have dropped the explanation.
        /// </summary>
        STX = 0x02,

        /// <summary>
        /// End of text; Terminates a text in a transmission. As per ASCII-1968, a text starts with STX and ends with ETX. Later standards don't necessarily require the pairing of STX with ETX.
        /// </summary>
        ETX = 0x03,

        /// <summary>
        /// End of transmission; Indicates the conclusion of a transmission. The transmission may have contained one or more texts and associated heading(s).
        /// </summary>
        EOT = 0x04,

        /// <summary>
        /// Carriage return
        /// </summary>
        CR = 0x0d,

        /// <summary>
        /// Line feed
        /// </summary>
        LF = 0x0a
    }
}