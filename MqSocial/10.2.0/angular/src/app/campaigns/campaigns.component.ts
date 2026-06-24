import { ChangeDetectorRef, Component, Injector, ViewChild } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { PagedListingComponentBase } from 'shared/paged-listing-component-base';
import {
    CampaignServiceProxy,
    CampaignDto,
    CampaignDtoPagedResultDto,
    CampaignStatus,
} from '@shared/service-proxies/service-proxies';
import { CreateCampaignDialogComponent } from './create-campaign/create-campaign-dialog.component';
import { EditCampaignDialogComponent } from './edit-campaign/edit-campaign-dialog.component';
import { Table, TableModule } from 'primeng/table';
import { LazyLoadEvent, PrimeTemplate } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { FormsModule } from '@angular/forms';
import { NgIf } from '@angular/common';
import { LocalizePipe } from '@shared/pipes/localize.pipe';

@Component({
    templateUrl: './campaigns.component.html',
    animations: [appModuleAnimation()],
    standalone: true,
    imports: [FormsModule, TableModule, PrimeTemplate, NgIf, PaginatorModule, LocalizePipe],
})
export class CampaignsComponent extends PagedListingComponentBase<CampaignDto> {
    @ViewChild('dataTable', { static: true }) dataTable: Table;
    @ViewChild('paginator', { static: true }) paginator: Paginator;

    campaigns: CampaignDto[] = [];
    keyword = '';
    filterStatus: CampaignStatus | undefined = undefined;
    advancedFiltersVisible = false;

    campaignStatuses = [
        { value: undefined, label: 'All' },
        { value: CampaignStatus.Draft, label: 'Draft' },
        { value: CampaignStatus.Active, label: 'Active' },
        { value: CampaignStatus.Paused, label: 'Paused' },
        { value: CampaignStatus.Completed, label: 'Completed' },
    ];

    constructor(
        injector: Injector,
        private _campaignService: CampaignServiceProxy,
        private _modalService: BsModalService,
        cd: ChangeDetectorRef
    ) {
        super(injector, cd);
    }

    createCampaign(): void {
        const modalRef: BsModalRef = this._modalService.show(CreateCampaignDialogComponent, {
            class: 'modal-lg',
        });
        modalRef.content.onSave.subscribe(() => {
            this.refresh();
        });
    }

    editCampaign(campaign: CampaignDto): void {
        const modalRef: BsModalRef = this._modalService.show(EditCampaignDialogComponent, {
            class: 'modal-lg',
            initialState: { id: campaign.id },
        });
        modalRef.content.onSave.subscribe(() => {
            this.refresh();
        });
    }

    clearFilters(): void {
        this.keyword = '';
        this.filterStatus = undefined;
        this.list();
    }

    getStatusLabel(status: CampaignStatus): string {
        const map = {
            [CampaignStatus.Draft]: 'Draft',
            [CampaignStatus.Active]: 'Active',
            [CampaignStatus.Paused]: 'Paused',
            [CampaignStatus.Completed]: 'Completed',
        };
        return map[status] ?? '';
    }

    getStatusClass(status: CampaignStatus): string {
        const map = {
            [CampaignStatus.Draft]: 'badge badge-secondary',
            [CampaignStatus.Active]: 'badge badge-success',
            [CampaignStatus.Paused]: 'badge badge-warning',
            [CampaignStatus.Completed]: 'badge badge-info',
        };
        return map[status] ?? 'badge badge-secondary';
    }

    list(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator.changePage(0);
            if (this.primengTableHelper.records && this.primengTableHelper.records.length > 0) {
                return;
            }
        }

        this.primengTableHelper.showLoadingIndicator();

        this._campaignService
            .getAll(
                this.keyword,
                this.filterStatus,
                this.primengTableHelper.getSorting(this.dataTable),
                this.primengTableHelper.getSkipCount(this.paginator, event),
                this.primengTableHelper.getMaxResultCount(this.paginator, event)
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result: CampaignDtoPagedResultDto) => {
                this.primengTableHelper.records = result.items;
                this.primengTableHelper.totalRecordsCount = result.totalCount;
                this.primengTableHelper.hideLoadingIndicator();
                this.cd.detectChanges();
            });
    }

    delete(campaign: CampaignDto): void {
        abp.message.confirm(
            this.l('CampaignDeleteWarningMessage', campaign.name),
            undefined,
            (result: boolean) => {
                if (result) {
                    this._campaignService.delete(campaign.id).subscribe(() => {
                        abp.notify.success(this.l('SuccessfullyDeleted'));
                        this.refresh();
                    });
                }
            }
        );
    }
}
