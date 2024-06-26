﻿using System.Collections.Generic;

namespace EasyOC.ContentExtensions.AppServices.Dtos;

public class ImportContentInput
{
    public List<ContentModel> InputList { get; set; }
    public string ContentType { get; set; }
    public bool Draft { get; set; } = false;
}
