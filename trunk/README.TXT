dkstools readme

1) extract data file (.bdt, .bhd5)
http://forum.xentax.com/viewtopic.php?f=10&t=7852

2) extract bdf's
assuming that all bdf's and bhf's are located in d:\ds\out:

  mkdir bdfout
  dksextr extractbdf d:\ds\out d:\ds\out
 
the first "d:\ds\out" is the location of bhf's to index for
guessing the names of the files to extracted from the bdf's.
all *.bhf files are scanned, non-recursively.

the second "d:\ds\out" is the location of the bdf's to extract
files from. all *.bdf files are processed, non-recursively.

dksextr will extract files to bdfout in the current working
directory (and crashes if directory does not exist).