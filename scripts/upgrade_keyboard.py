import os
import argparse
import xml.etree.ElementTree as ET
from xml.etree.ElementTree import Element, SubElement, Comment, tostring


def print_elems(root, indent=0):
    indent_string = "\t"*indent
    for r in root:
        print(indent_string+str(r))
        print_elems(r, indent+1)

        
def items_for_upgrade():
    ########################
    # List of name changes #
    ########################
    swaps = {}

    # Different key types all merged to "DynamicKey"
    swaps["ActionKey"] = "DynamicKey"
    swaps["ChangeKeyboardKey"] = "DynamicKey"
    swaps["TextKey"] = "DynamicKey"

    # Some renaming
    swaps["DestinationKeyboard"] = "ChangeKeyboard"
    swaps["ReturnToThisKeyboard"] = "BackReturnsHere"

    # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # #
    # List of elements that have been promoted to parents's attributes  #
    # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # 
    promote_attribs = []
    promote_attribs.append("Row")
    promote_attribs.append("Col")
    promote_attribs.append("Width")
    promote_attribs.append("Height")

    # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # #
    # List of elements that have been promoted to sibling's attributes  #
    # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # #
    sideways_attribs = {}
    # key: element that's becoming an attrib, 
    # value: the sibling element the attrib will live in
    sideways_attribs["BackReturnsHere"] = "ChangeKeyboard" 

    return swaps, promote_attribs, sideways_attribs


def upgrade_file(fname_in, fname_out, verbose):

    tree = ET.parse(fname_in)

    swaps, promote_attribs, sideways_attribs = items_for_upgrade()

    # Rename elements
    for elem in tree.getroot().getiterator():    
        if elem.tag in swaps:
            if verbose:
                print("swapping \"{}\" -> \"{}\"".format(elem.tag, swaps[elem.tag]))
            elem.tag = swaps[elem.tag]

    # Promote elements to attributes
    for attr in promote_attribs:
        # Find any node with matching child element     
        parents = tree.findall(".//{}/..".format(attr))    

        for parent in parents:        
            if verbose:
                print("Promoting element \"{}\" as attribute of \"{}\"".format(attr, parent.tag))
            # get value from child element
            child = parent.find(attr)                  
            val = child.text

            # set matching attribute on parent
            parent.set(attr, val)         

            # remove child element
            parent.remove(child)

    # Shift elements to sibling attributes
    for attr in sideways_attribs:
        # Find any node with matching child element     
        parents = tree.findall(".//{}/..".format(attr))    

        for parent in parents:   
            
            # find sibling
            sibling = parent.find(sideways_attribs[attr])   
            if verbose:
                print("Shifting element \"{}\" as attribute of \"{}\"".format(attr, sideways_attribs[attr]))
            
            # get value from child element
            child = parent.find(attr)  
            val = child.text
            
            # set matching attribute on sibling
            if sibling is not None:
                sibling.set(attr, val)         
            else:             
                print("WARNING: Cannot find sibling \"{}\" - is this a blank/incomplete key?\n".format(sideways_attribs[attr]))             

            # remove child element
            parent.remove(child)

    # Save out modified tree
    tree.write(fname_out)


if __name__ == '__main__':

    sim_types = ["d", "p", "t"]

    # pylint: disable=invalid-name
    parser = argparse.ArgumentParser()
    parser.add_argument("input_file_or_folder", type=str)        
    parser.add_argument("new_file_or_folder", nargs='?', default="output", type=str)        
    parser.add_argument('-v', '--verbose', dest='verbose', action='store_true')    

    args = parser.parse_args()

    # TODO: maintain folder hierarchy    
    if os.path.isdir(args.input_file_or_folder):
        print("Found a directory, will upgrade all files")

        output_dir = args.new_file_or_folder        
        root_path = args.input_file_or_folder

        for subdir, dirs, files in os.walk(root_path):
            for file in files:
                
                # We'll maintain folder hierarchy for output
                relpath = os.path.relpath(subdir, root_path)
                
                if file.endswith(".xml"):
                    orig_file = os.path.join(subdir, file)
                    new_dir = os.path.join(output_dir, relpath)
                    new_file = os.path.join(new_dir, file)

                    if not os.path.exists(new_dir):
                        os.makedirs(new_dir)

                    print("parsing: \n{} \nand saving new file as: \n{}\n".format(orig_file, new_file))
                    upgrade_file(orig_file, new_file, args.verbose)

    else:
        print("found a single file")
        new_file = args.new_file_or_folder        
        orig_file = args.input_file_or_folder

        if not new_file.lower().endswith(".xml"):
            new_file = new_file + ".xml"        

        print("parsing: \n{} \nand saving new file as: \n{}\n".format(orig_file, new_file))
        upgrade_file(orig_file, new_file, args.verbose)


    

