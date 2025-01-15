**Overview**

LabelCast is software which allows printing of barcode labels to Zebra label printers, based on data manually input or supplied by a database. Typically, you would input one or more data fields, which are used to look up additional information in a database and then combined to print a barcode label.

At this time, only label printers from the company Zebra are supported.

The software comes in two flavors:

*   Windows desktop application
*   Web application

If you have a single workstation with a dedicated Zebra label printer, or just a few workstations, each of which has their own label printer where each workstation has individual label needs, the simplest solution is to install the desktop application on each workstation.

If multiple users need to print the same type of label, or if they share printers, or if diverse types of devices exist which need to print labels, the web application is a better fit.

**Why This Application?**

A lot of labeling software exists. However, most of them are intended for general purpose labelling and are using Windows printer drivers. This is fine, but Zebra printers are special case – using the Windows printer driver to print barcode labels often results in barcodes which cannot be properly read in all barcode readers.

To properly print barcodes on Zebra printers, the native Zebra printer language ZPL must be employed to send the label. This app is intended to do just that.

**Label Design and Workflow**

LabelCast does not support designing label templates. This must be done by a separate program, such as the free _Zebra Designer_ from the Zebra website.

The work flow is as follows:

1.  Install Zebra Designer for Developers
2.  Create a label design
3.  Save the label design as a print template file – a text file containing Zebra ZPL commands.
4.  Install LabelCast desktop or web application.
5.  Configure LabelCast to use your label design, specifying database details and fields to be input by the user.
6.  Print the labels using either the desktop app or web application.

**HTTP API for JSON and XML**

You can also configure LabelCast to print labels using a HTTP API. You can submit label data in XML or JSON format. Optionally, the label data format can be validated through a XML / JSON schema.

This is only supported when the web application has been installed.