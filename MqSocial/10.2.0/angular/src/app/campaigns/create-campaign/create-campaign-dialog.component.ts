import { Component, Injector, OnInit, EventEmitter, Output, ChangeDetectorRef } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import {
    CampaignServiceProxy,
    CreateCampaignDto,
    CampaignStatus,
    CompanyServiceProxy,
    CompanyDto,
} from '@shared/service-proxies/service-proxies';
import { FormsModule } from '@angular/forms';
import { AbpModalHeaderComponent } from '../../../shared/components/modal/abp-modal-header.component';
import { AbpModalFooterComponent } from '../../../shared/components/modal/abp-modal-footer.component';
import { AbpValidationSummaryComponent } from '../../../shared/components/validation/abp-validation.summary.component';
import { LocalizePipe } from '@shared/pipes/localize.pipe';
import moment from 'moment';

@Component({
    templateUrl: './create-campaign-dialog.component.html',
    standalone: true,
    imports: [
        FormsModule,
        AbpModalHeaderComponent,
        AbpValidationSummaryComponent,
        AbpModalFooterComponent,
        LocalizePipe,
    ],
})
export class CreateCampaignDialogComponent extends AppComponentBase implements OnInit {
    @Output() onSave = new EventEmitter<any>();

    saving = false;
    campaign = new CreateCampaignDto();
    companies: CompanyDto[] = [];

    startDateStr: string = '';
    endDateStr: string = '';

    campaignStatuses = [
        { value: CampaignStatus.Draft, label: 'Draft' },
        { value: CampaignStatus.Active, label: 'Active' },
        { value: CampaignStatus.Paused, label: 'Paused' },
        { value: CampaignStatus.Completed, label: 'Completed' },
    ];

    constructor(
        injector: Injector,
        public _campaignService: CampaignServiceProxy,
        public _companyService: CompanyServiceProxy,
        public bsModalRef: BsModalRef,
        private cd: ChangeDetectorRef
    ) {
        super(injector);
    }

    ngOnInit(): void {
        this.campaign.status = CampaignStatus.Draft;
        this._companyService.getAll(undefined, undefined, 0, 1000).subscribe((result) => {
            this.companies = result.items;
            this.cd.detectChanges();
        });
    }

    save(): void {
        this.saving = true;
        this.campaign.startDate = this.startDateStr ? moment(this.startDateStr) : undefined;
        this.campaign.endDate = this.endDateStr ? moment(this.endDateStr) : undefined;

        this._campaignService.create(this.campaign).subscribe({
            next: () => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.bsModalRef.hide();
                this.onSave.emit();
            },
            error: () => { this.saving = false; },
        });
    }
}
