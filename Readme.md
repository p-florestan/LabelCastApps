**Overview**

LabelCast is software which allows printing of barcode labels to label printers supporting ZPL (Zebra Printer Language). This includes all printers from the company Zebra, but other manufacturers such as TSC (Taiwan Semiconductor), Intermec (Honeywell), Sato, Bixolon and Cab also offer printers which support or can emulate ZPL.

The software UI allows you to select a barcode label design and then print by:

- manually typing variable data into the user interface.
- selecting predefined data from a database.
- both – i.e. selecting data from a database and manually inputting further details

The software comes in two flavors:

- Windows desktop application
- Web application

If you have a single workstation with a dedicated label printer, or just a few workstations with individual label needs, the simplest solution is to install the desktop application on each workstation.

If multiple users need to print the same type of label, or if they share printers, if diverse types of devices exist which need to print labels, or you want to manage label printing for geographically separate offices, the web application is a better fit.

**Intended Use**

This software is intended to print labels containing barcodes to a networked barcode printer.

General-purpose labelling software usually converts the label data into an image and lets the printer driver translate this to pixels for the printer. Barcodes do not print properly this way, however, on typical industrial label printers from Zebra or TSC etc. – these printers often have much lower resolution than a typical modern laser or inkjet printer, resulting in barcodes which cannot be reliably read by a barcode reader.

The advantage of the lower resolution is speedy output of complex barcode labels. But the only way to create properly printed barcodes on such printers is to use their proprietary printer language, such as ZPL.

The LabelCast software directly sends ZPL commands to the printer and thereby creates barcodes which can be reliably read by barcode readers.

**Label Design and Workflow**

LabelCast does not support designing label templates. This must be done by a separate program, such as the free program *Zebra Designer for Developers* from the Zebra website.

The work flow is as follows:

1. Install Zebra Designer for Developers
1. Create a label design
1. Save the label design as a print template file – a text file containing Zebra ZPL commands.
1. Install LabelCast desktop or web application.
1. Configure LabelCast to use your label design, specifying database details and fields to be input by the user.
1. Print the labels using either the desktop app or web application.

**HTTP API for JSON and XML**

You can also configure LabelCast to print labels using HTTP API requests. You can submit label data in XML or JSON format. Optionally, the label data format can be validated through a XML / JSON schema.

You must install the web application to take advantage of this feature.

**Detailed User Manual**

A detailed user manual is contained in the /docs folder of the repository.


