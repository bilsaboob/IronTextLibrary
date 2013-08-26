import math
import re
import string
from collections import defaultdict

SRC_FILE = 'UnicodeIntSetType.Data.cs'
START_SRC = """\
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronText.Algorithm
{
    /// <summary>
    /// Don't edit this file! It was generated by the 'ucd2cs.py'.
    /// You can find script in the same directory.
    /// </summary>
    public partial class UnicodeIntSetType
    {
        private void InitMainGeneralCategories()
        {
"""

END_SRC = """
        }
    }
}
"""

categoryToProperty = {
    'Lu' : 'UppercaseLetter',
    'Ll' : 'LowercaseLetter',
    'Lt' : 'TitlecaseLetter',
    'Lm' : 'ModifierLetter',
    'Lo' : 'OtherLetter',
    'Mn' : 'NonSpacingMark',
    'Mc' : 'SpacingCombiningMark',
    'Me' : 'EnclosingMark',
    'Nd' : 'DecimalDigitNumber',
    'Nl' : 'LetterNumber',
    'No' : 'OtherNumber',
    'Zs' : 'SpaceSeparator',
    'Zl' : 'LineSeparator',
    'Zp' : 'ParagraphSeparator',
    'Cc' : 'Control',
    'Cf' : 'Format',
    'Cs' : 'Surrogate',
    'Co' : 'PrivateUse',
    'Cn' : 'Unassigned',
    'Pc' : 'ConnectorPunctuation',
    'Pd' : 'DashPunctuation',
    'Ps' : 'OpenPunctuation',
    'Pe' : 'ClosePunctuation',
    'Pi' : 'InitialQuotePunctuation',
    'Pf' : 'FinalQuotePunctuation',
    'Po' : 'OtherPunctuation',
    'Sm' : 'MathSymbol',
    'Sc' : 'CurrencySymbol',
    'Sk' : 'ModifierSymbol',
    'So' : 'OtherSymbol'
    }

def parseDatabase(sourceFile, storeAsStrings=False):
    charDict = {}

    with open(sourceFile) as uni:
        flag = False
        first = 0
        for line in uni:
            d = string.split(line.strip(), ';')
            val = int(d[0], 16)
            if flag:
                if re.compile('<.+, Last>').match(d[1]):
                    # print '%s: u%X' % (d[1], val)
                    flag = False
                    for t in range(first, val + 1):
                        charDict[t] = str(d[2])
                else:
                    raise 'Database exception'
            else:
                if re.compile('<.+, First>').match(d[1]):
                    # print '%s: u%X' % (d[1], val)
                    flag = True
                    first = val
                else:
                    charDict[val] = str(d[2])

    # http://unicode.org/reports/tr44/#GC_Values_Table
    # http://unicode.org/reports/tr18/#Categories
    categoryDict = defaultdict(list)
    for codePoint in range(0x10FFFF + 1):
        if charDict.get(codePoint) == None:
            categories = ['C', 'Cn']
        else:
            cat = charDict[codePoint]
            categories = [cat, cat[0]]
        for category in categories:
            l = categoryDict[category]
            if len(l) == 0:
                l.append((codePoint, codePoint))
            elif l[-1][1] == codePoint - 1:
                l[-1] = (l[-1][0], codePoint)
            elif l[-1][1] > codePoint:
                print "Error:", str(categories) + " at " + hex(codePoint)
            else:
                l.append((codePoint, codePoint))
    return categoryDict

def main():
    categoryToRanges = parseDatabase("UnicodeData.txt")
    with open(SRC_FILE, "w") as output:
        output.write(START_SRC)
        for category, ranges in categoryToRanges.items():
            if len(category) == 1:
                continue
            prop = categoryToProperty.get(category)
            if prop is None:
                raise Exception, "Category %s has no associated property." % category
            output.write("\n            %s = Ranges(new IntInterval[] {" % prop)
            isFirst = True
            for first, last in ranges:
                if not isFirst:
                    output.write(",")
                if first == last:
                    output.write("\n                new IntInterval(0x%04x)" % first)
                else:
                    output.write("\n                new IntInterval(0x%04x, 0x%04x)" % (first, last))
                isFirst = False
            output.write("\n            });\n")

        output.write(END_SRC)

    
main()
