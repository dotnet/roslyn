#!/usr/bin/env python2.7

import os
import sys
import textwrap

def post_to_github(context, state, description=None):
    if 'GITHUB_TOKEN' not in os.environ:
        print 'NOT posting results to GitHub ($GITHUB_TOKEN not available)'
        return

    payload = dict(
        context=context,
        state=state,
        #target_url="{CI_PROJECT_URL}/-/jobs/{CI_BUILD_ID}".format(**os.environ),
        target_url="{CI_PROJECT_URL}/pipelines/{CI_PIPELINE_ID}".format(**os.environ),
    )

    if description:
        payload.update(dict(description=description))

    import requests
    print 'sending status to github...'
    print 'sending to: {GITHUB_REPO_API}/statuses/{CI_COMMIT_SHA}'.format(**os.environ)
    print 'Bearer {GITHUB_TOKEN}'.format(**os.environ)
    response = requests.post(
        '{GITHUB_REPO_API}/statuses/{CI_COMMIT_SHA}'.format(**os.environ),
        headers={'Authorization': 'Bearer {GITHUB_TOKEN}'.format(**os.environ)},
        json=payload,
    )
    print response.text

os.system('pip install requests')
post_to_github(context=sys.argv[1], state=sys.argv[2])
