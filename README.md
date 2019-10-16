# Photo Reconciler

## Background

In the middle of migrating 400 GB and 25k photos to Lightroom Creative Cloud, I realized it was making an additional copy of all my files in the target library's "originals" folder. Fortunately, I had just enough disk space on the target drive (external SSD) to fit all these files. But, to free up disk space now that the migration is complete, I decided to delete one set of files.

However, my "source" directory has taken on various forms over the years, transitioning from Picasa to Lightroom around 2010 ... and I probably managed photos some other way before that. I had a low degree of confidence that my Lightroom library really contained all the files that are on disk.

Not wanting to lose any of my files, and desperately missing C# now that I'm writing Ruby, I threw together some code to compare my source and destination directories and to identify discrepancies.

I don't expect to need this code again, but maybe you've run across the same problem and this will help you. My apologies for the rough code and hard-coded values.

## Matching files between source and destination

On migration, Lightroom seemed to move files to different folder names when their capture time was close to a date boundary. I didn't look carefully enough to confirm, but I think Lightroom CC may be organizing by UTC time, whereas Lightroom Classic previously organized according to my local time. Regardless, I found a file-path-based approach for matching files to be an insufficient heuristic, so I stuck with filename and file size. Filesystem lookups for size seemed to be rather quick on SSD, and I preferred to avoid computing a hash for every file. The script outputs duplicates; I had only 34 in my input and a quick scan confirmed these are true duplicates. YMMV if you're comparing something other than photos where names are more likely to collide.

## Future Improvements

I was tempted to go crazy with this, generalize the solution, even [implement a CLI](https://natemcmaster.github.io/CommandLineUtils/), etc. Maybe if I run out of other things to do in my free time . . .
