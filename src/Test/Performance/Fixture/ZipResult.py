import os
import sys
import zipfile

try:
    import zlib
    compression = zipfile.ZIP_DEFLATED
except:
    compression = zipfile.ZIP_STORED

modes = { zipfile.ZIP_DEFLATED: 'deflated',
          zipfile.ZIP_STORED:   'stored',
          }

zf = zipfile.ZipFile(sys.argv[2], mode='w')
try:
    zf.write(sys.argv[1], compress_type=compression)
finally:
    zf.close()
