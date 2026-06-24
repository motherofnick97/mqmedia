import { Component, Injector, OnInit, EventEmitter, Output, ChangeDetectorRef } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import {
    CampaignServiceProxy,
    CampaignDto,
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
    templateUrl: './edit-campaign-dialog.component.html',
    standalone: true,
    imports: [
        FormsModule,
        AbpModalHeaderComponent,
        AbpValidationSummaryComponent,
        AbpModalFooterComponent,
        LocalizePipe,
    ],
})
export class EditCampaignDialogComponent extends AppComponentBase implements OnInit {
    @Output() onSave = new EventEmitter<any>();

    saving = false;
    campaign = new CampaignDto();
    companies: CompanyDto[] = [];
    id: string;

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
        this._companyService.getAll(undefined, undefined, 0, 1000).subscribe((companies) => {
            this.companies = companies.items;
            this._campaignService.get(this.id).subscribe((result) => {
                this.campaign = result;
                this.startDateStr = result.startDate ? result.startDate.format('YYYY-MM-DD') : '';
                this.endDateStr = result.endDate ? result.endDate.format('YYYY-MM-DD') : '';
                this.cd.detectChanges();
            });
        });
    }

    save(): void {
        this.saving = true;
        this.campaign.startDate = this.startDateStr ? moment(this.startDateStr) : undefined;
        this.campaign.endDate = this.endDateStr ? moment(this.endDateStr) : undefined;

        this._campaignService.update(this.campaign).subscribe({
            next: () => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.bsModalRef.hide();
                this.onSave.emit();
            },
            error: () => { this.saving = false; },
        });
    }
}
