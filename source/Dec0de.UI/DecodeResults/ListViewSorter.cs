/**
 * Copyright (C) 2012 University of Massachusetts, Amherst.
 * Brian Lynn
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Windows.Forms;

namespace Dec0de.UI.DecodeResults
{
    class ListViewSorter : IComparer
    {
        public enum ViewType : int { CALLLOG, ADRBOOK, SMS, IMAGES }
        private enum SortType : int { PHONENUMBER, NUMBER, TEXT, TEXT_NOCASE, TIMESTAMP, CHECKBOX, REALNUM }
        private ViewType viewType;
        private int column;
        private SortOrder sortOrder;
        private SortType sortType;

        /**
         * Sets the column, sort order (ascending/descending), and type of
         * view.
         */
        public ListViewSorter(int column, SortOrder sortOrder, ViewType viewType)
        {
            this.column = column;
            this.sortOrder = sortOrder;
            this.viewType = viewType;
            // How we sort depends on the view and column.
            SortType how = SortType.TEXT;
            switch (viewType) {          
                case ViewType.CALLLOG:
                    switch (column) {
                        case 0:
                            how = SortType.CHECKBOX;
                            break;
                        case 1:
                            how = SortType.PHONENUMBER;
                            break;
                        case 2:
                            how = SortType.TEXT_NOCASE;
                            break;
                        case 4:
                            how = SortType.TIMESTAMP;
                            break;
                        default:
                            how = SortType.TEXT;
                            break;
                    }
                    break;
                case ViewType.ADRBOOK:
                    switch (column) {
                        case 0:
                            how = SortType.CHECKBOX;
                            break;
                        case 1:
                            how = SortType.TEXT_NOCASE;
                            break;
                        case 2:
                            how = SortType.PHONENUMBER;
                            break;
                        default:
                            how = SortType.TEXT;
                            break;
                    }
                    break;
                case ViewType.SMS:
                    switch (column) {
                        case 0:
                            how = SortType.CHECKBOX;
                            break;
                        case 1:
                        case 2:
                            how = SortType.PHONENUMBER;
                            break;
                        case 4:
                            how = SortType.TIMESTAMP;
                            break;
                        default:
                            how = SortType.TEXT;
                            break;
                    }
                    break;
                case ViewType.IMAGES:
                    switch (column) {
                        case 0:
                            how = SortType.CHECKBOX;
                            break;
                        case 3:
                        case 4:
                            how = SortType.NUMBER;
                            break;
                        default:
                            how = SortType.TEXT;
                            break;
                    }
                    break;
            }
            sortType = how;
        }

        /**
         * Compares values for sort order.
         */
        public int Compare(object x, object y)
        {
            string xText = null;
            string yText = null;
            if (sortType != SortType.CHECKBOX) {
                xText = ((ListViewItem)x).SubItems[column].Text;
                yText = ((ListViewItem)y).SubItems[column].Text;
            } else {
                xText = ((ListViewItem)x).Name;
                yText = ((ListViewItem)y).Name;
            }
            int sign = (sortOrder == SortOrder.Ascending) ? 1 : -1;
            // How we sort depends on the view and column.     
            try {
                if (sortType == SortType.TEXT) {
                    // Text is simple ...
                    return String.Compare(xText, yText, false)*sign;
                } else if (sortType == SortType.TEXT_NOCASE) {
                    int n = String.Compare(xText, yText, true)*sign;
                    if (n != 0) {
                        return n;
                    } else {
                        return String.Compare(xText, yText, false)*sign;
                    }
                } else if (sortType == SortType.PHONENUMBER) {
                    // Phone number. Right justify before compare.
                    return String.Compare(String.Format("{0,30}", xText), String.Format("{0,30}", yText), true) * sign;
                } else if (sortType == SortType.NUMBER) {
                    // Integer value.
                    long xInt = String.IsNullOrEmpty(xText) ? 0 : long.Parse(xText);
                    long yInt = String.IsNullOrEmpty(yText) ? 0 : long.Parse(yText);
                    long ldif = (xInt - yInt) * sign;
                    if (ldif < 0) {
                        return -1;
                    } else if (ldif > 0) {
                        return 1;
                    } else {
                        return 0;
                    }
                } else if (sortType == SortType.CHECKBOX) {
                    // The checkbox column.
                    if (!((ListViewItem)x).Checked && ((ListViewItem)y).Checked) {
                        return sign;
                    } else if (((ListViewItem)x).Checked && !((ListViewItem)y).Checked) {
                        return sign * -1;
                    }
                    // Both checked or unchecked. Use the name as the tiebreaker.
                    return String.Compare(xText, yText, false);
                } else if (sortType == SortType.REALNUM) {
                    // Floating point number.
                    double xFloat = String.IsNullOrEmpty(xText) ? 0 : double.Parse(xText);
                    double yFloat = String.IsNullOrEmpty(yText) ? 0 : double.Parse(yText);
                    double fTmp = ((xFloat - yFloat)) * sign;
                    if (fTmp < 0) {
                        return -1;
                    } else if (fTmp > 0) {
                        return 1;
                    } else {
                        return 0;
                    }
                } else if (sortType == SortType.TIMESTAMP) {
                    // Timestamp. An empty string (missing timestamp) comes after a filled in timestamp.
                    if (String.IsNullOrEmpty(xText)) xText = "ZZZZ-ZZ-ZZ ZZ:ZZ:ZZ";
                    if (String.IsNullOrEmpty(yText)) yText = "ZZZZ-ZZ-ZZ ZZ:ZZ:ZZ";
                    return String.Compare(xText, yText, true) * sign;
                } else {
                    return 0;
                }
            } catch {
                return 0;
            }
        }

    }
}
